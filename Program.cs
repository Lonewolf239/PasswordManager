using System;
using System.IO;
using System.Text;
using System.Threading;
using PasswordManager.Classes;
using System.Runtime.InteropServices;

namespace PasswordManager
{
	public static class Program
	{
		public static string Version = "1.0";

		public static string GetPassword(bool hide = true)
		{
			Console.CursorVisible = true;
			StringBuilder password = new();
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Enter) break;
				else if (key.Key == ConsoleKey.Backspace)
				{
					if (password.Length > 0)
					{
						password = password.Remove(password.Length - 1, 1);
						Console.Write("\u001b[D \u001b[D");
					}
				}
				else if (!char.IsControl(key.KeyChar))
				{
					password.Append(key.KeyChar);
					if (hide) Console.Write('*');
					else Console.Write(key.KeyChar);
				}
			}
			Console.WriteLine();
			Console.CursorVisible = false;
			return password.ToString();
		}

		private static void OnApplicationClosing(Passwords passwords)
		{
			Console.Clear();
			Console.CursorVisible = true;
			FileManager.Save(passwords.ToString());
		}

		private static async Task Main()
		{
			Console.Clear();
			Console.CursorVisible = false;
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Console.WriteLine("This application is only supported on Linux.");
				return;
			}
			await UpdateManager.CheckAndUpdate();
			bool firstStart = false;
			string data = FileManager.Read();
			Passwords passwords = new();
			if (data == "[]") firstStart = true;
			else passwords = new(data);
			int attemps = 0;
			while (true)
			{
				Console.Clear();
				Console.Write("Enter password: ");
				string password = GetPassword(!firstStart);
				if (firstStart)
				{
					passwords.ChangeMainPassword(password);
					break;
				}
				if (!passwords.CheckMainPassword(password))
				{
					Console.WriteLine("Incorrect password!");
					attemps++;
					if (attemps == 3)
					{
						Console.WriteLine("\nDo you want to restore access? [Y/N]: ");
						Console.WriteLine("WARNING: This action will erase all data.");
						var recovery = Console.ReadKey(true);
						if (recovery.Key == ConsoleKey.Y)
						{
							Console.Write("Enter new password: ");
							password = GetPassword(false);
							passwords.Clear(password);
							FileManager.Save(passwords.ToString());
							break;
						}
					}
					else Thread.Sleep(500);
				}
				else break;
			}
			AppDomain.CurrentDomain.ProcessExit += (s, e) => OnApplicationClosing(passwords);
			Manager manager = new(passwords);
			manager.MainMenu();
		}
	}
}
