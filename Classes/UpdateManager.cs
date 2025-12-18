using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PasswordManager.Classes
{
	public static class UpdateManager
	{
		private const string VersionUrl = "https://base-escape.ru/PasswordManager/version.txt";
		private const string DownloadUrl = "https://base-escape.ru/PasswordManager/";
		private static readonly HttpClient Client = new();

		public static async Task CheckAndUpdate()
		{
			try
			{
				UI.PrintHeader("CHECKING FOR UPDATES");
				string? remoteVersion = await GetRemoteVersion();
				string currentVersion = Program.Version;
				if (remoteVersion == null) return;
				if (remoteVersion == currentVersion) return;
				Console.WriteLine($"📌 Current version: {currentVersion}");
				Console.WriteLine($"🌐 Server version: {remoteVersion}\n");
				Console.WriteLine("Would you like to update the application? [Y/N]: ");
				while (true)
				{
					var key = Console.ReadKey(true).Key;
					if (key == ConsoleKey.Y)
					{
						await DownloadAndInstallUpdate();
						return;
					}
					if (key == ConsoleKey.N) break;
				}
			}
			catch {}
		}

		private static async Task<string?> GetRemoteVersion()
		{
			var response = await Client.GetAsync(VersionUrl);
			if (response.IsSuccessStatusCode)
			{
				string content = await response.Content.ReadAsStringAsync();
				return content.Trim();
			}
			return null;
		}

		private static async Task DownloadAndInstallUpdate()
		{
			string? currentExePath = GetCurrentExecutablePath();
			if (string.IsNullOrEmpty(currentExePath)) return;
			string fileName = "PasswordManager";
			string downloadUrl = DownloadUrl + fileName;
			string tempPath = Path.Combine(Path.GetTempPath(), fileName + ".tmp");
			if (!File.Exists(currentExePath))
			{
				string[] possiblePaths = new[]
				{
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PasswordManager"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin/PasswordManager"),
					"/usr/local/bin/PasswordManager"
				};
				currentExePath = null;
				foreach (var path in possiblePaths)
				{
					if (File.Exists(path))
					{
						currentExePath = path;
						break;
					}
				}
				if (currentExePath == null) return;
			}
			Console.Clear();
			UI.PrintHeader("DOWNLOADING THE UPDATE");
			await DownloadFile(downloadUrl, tempPath);
			if (!File.Exists(tempPath)) return;
			var fileInfo = new FileInfo(tempPath);
			if (fileInfo.Length == 0)
			{
				File.Delete(tempPath);
				return;
			}
			await Update(tempPath, currentExePath);
		}

		private static async Task DownloadFile(string url, string filePath)
		{
			try
			{
				using (var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
				{
					if (!response.IsSuccessStatusCode) throw new Exception();
					var totalBytes = response.Content.Headers.ContentLength ?? 0L;
					var canReportProgress = totalBytes != 0;
					using (var contentStream = await response.Content.ReadAsStreamAsync())
					using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
					{
						var totalRead = 0L;
						var buffer = new byte[8192];
						int read;
						while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
						{
							await fileStream.WriteAsync(buffer, 0, read);
							totalRead += read;
							if (canReportProgress)
							{
								var percentage = (int)((totalRead * 100) / totalBytes);
								DrawProgressBar(percentage);
							}
						}
						await fileStream.FlushAsync();
					}
				}
				Console.WriteLine();
			}
			catch { if (File.Exists(filePath)) File.Delete(filePath); }
		}

		private static void DrawProgressBar(int percentage)
		{
			int barLength = 29;
			int filledLength = (barLength * percentage) / 100;
			ConsoleColor barColor = ConsoleColor.Green;
			Console.Write("\rProgress: ├");
			Console.ForegroundColor = barColor;
			Console.Write(new string('━', filledLength));
			if (filledLength < barLength)
			{
				Console.Write('┫');
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.Write(new string('─', barLength - filledLength - 1));
			}
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write($"┤ {percentage,2}%");
			Console.ResetColor();
		}

		private static async Task Update(string newFilePath, string currentExePath)
		{
			string backupPath = currentExePath + ".backup";
			string scriptPath = Path.Combine(Path.GetTempPath(), "update_pm.sh");
			try
			{
				var fileInfo = new FileInfo(newFilePath);
				if (fileInfo.Length == 0) throw new Exception();
				string escapedCurrent = EscapePathForBash(currentExePath);
				string escapedBackup = EscapePathForBash(backupPath);
				string escapedNew = EscapePathForBash(newFilePath);
				string escapedScript = EscapePathForBash(scriptPath);
				string scriptContent = $@"#!/bin/bash
					set -e
					exec 1> {escapedScript}.log 2>&1
					sleep 2
					if [ ! -f {escapedNew} ]; then
						exit 1
					fi
					if [ -f {escapedCurrent} ]; then
						cp -f {escapedCurrent} {escapedBackup} 2>/dev/null
					fi
					if ! cp -f {escapedNew} {escapedCurrent}; then
						if [ -f {escapedBackup} ]; then
							cp -f {escapedBackup} {escapedCurrent}
						fi
						exit 1
					fi
					chmod +x {escapedCurrent}
					rm -f {escapedBackup}
					rm -f {escapedNew}
					nohup {escapedCurrent} > /dev/null 2>&1 &
					sleep 2
					rm -f {escapedScript}
					rm -f {escapedScript}.log
				";
				Directory.CreateDirectory(Path.GetDirectoryName(scriptPath) ?? Path.GetTempPath());
				File.WriteAllText(scriptPath, scriptContent);
				var chmodProcess = new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = $"-c \"chmod +x '{scriptPath}'\"",
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};
				using (var process = Process.Start(chmodProcess))
				{
					if (!process?.WaitForExit(5000) ?? false)
					{
						process?.Kill();
						throw new Exception();
					}
				}
				var updateProcess = new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = scriptPath,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = false,
					RedirectStandardError = false
				};
				Process.Start(updateProcess);
				await Task.Delay(500);
				Environment.Exit(0);
			}
			catch
			{
				try { if (File.Exists(backupPath)) File.Copy(backupPath, currentExePath, true); }
				catch {}
			}
			try
			{
				if (File.Exists(newFilePath)) File.Delete(newFilePath);
				if (File.Exists(scriptPath)) File.Delete(scriptPath);
			}
			catch {}
		}

		private static string EscapePathForBash(string path) => "'" + path.Replace("'", "'\\''") + "'";

		private static string GetCurrentExecutablePath()
		{
			try
			{
				string location = System.AppContext.BaseDirectory;
				if (string.IsNullOrEmpty(location)) location = Process.GetCurrentProcess().MainModule?.FileName ?? "";
				return location;
			}
			catch { return Process.GetCurrentProcess().MainModule?.FileName ?? ""; }
		}
	}
}
