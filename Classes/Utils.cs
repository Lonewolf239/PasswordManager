using System;
using System.Text;
using System.Security.Cryptography;

namespace PasswordManager.Classes
{
	public static class Crypto 
	{
		public static string Encrypt(string plainText, byte[] key)
		{
			using (var aes = Aes.Create())
			{
				aes.Key = key;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				aes.GenerateIV();
				var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
				using (var ms = new MemoryStream())
				{
					ms.Write(aes.IV, 0, aes.IV.Length);
					using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					using (var sw = new StreamWriter(cs)) sw.Write(plainText);
					return Convert.ToBase64String(ms.ToArray());
				}
			}
		}

		public static string Decrypt(string cipherText, byte[] key)
		{
			var buffer = Convert.FromBase64String(cipherText);
			using (var aes = Aes.Create())
			{
				aes.Key = key;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				var iv = new byte[aes.IV.Length];
				if (buffer.Length < iv.Length) return "[]"; 
				Array.Copy(buffer, 0, iv, 0, iv.Length);
				aes.IV = iv;
				var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
				using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
				using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				using (var sr = new StreamReader(cs)) return sr.ReadToEnd();
			}
		}
	}

	public static class Hasher
	{
		private const int SaltSize = 16;
		private const int KeySize = 32;
		private const int Iterations = 10000;

		public static string Get(string input)
		{
			var salt = new byte[SaltSize];
			using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
			using (var pbkdf2 = new Rfc2898DeriveBytes(input, salt, Iterations, HashAlgorithmName.SHA256))
			{
				var hash = pbkdf2.GetBytes(KeySize);
				var hashWithSalt = new byte[SaltSize + KeySize];
				Array.Copy(salt, 0, hashWithSalt, 0, SaltSize);
				Array.Copy(hash, 0, hashWithSalt, SaltSize, KeySize);
				return Convert.ToBase64String(hashWithSalt);
			}
		}

		public static bool Verify(string input, string storedHash)
		{
			if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(storedHash)) return false;
			try
			{
				var hashWithSalt = Convert.FromBase64String(storedHash);
				if (hashWithSalt.Length != SaltSize + KeySize) return false;
				var salt = new byte[SaltSize];
				Array.Copy(hashWithSalt, 0, salt, 0, SaltSize);
				using (var pbkdf2 = new Rfc2898DeriveBytes(input, salt, Iterations, HashAlgorithmName.SHA256))
				{
					var hash = pbkdf2.GetBytes(KeySize);
					var storedHashOnly = new byte[KeySize];
					Array.Copy(hashWithSalt, SaltSize, storedHashOnly, 0, KeySize);
					return CryptographicOperations.FixedTimeEquals(hash, storedHashOnly);
				}
			}
			catch { return false; }
		}
	}

        public static class Colors
        {
            public const string Success = "\u001b[32m";
            public const string Warning = "\u001b[33m";
            public const string Error = "\u001b[31m";
            public const string Info = "\u001b[36m";
            public const string Reset = "\u001b[0m";
            public const string Bold = "\u001b[1m";
        }

	public static class UI
	{
		public static void DisplayMainMenu()
		{
			Console.Clear();
			Console.WriteLine("╔═══════════════════════════════════════════╗");
			Console.WriteLine($"║              {Colors.Bold}PASSWORD MANAGER{Colors.Reset}             ║");
			Console.WriteLine("║              By. Lonewolf239              ║");
			Console.WriteLine("╠═══════════════════════════════════════════╣");
			Console.WriteLine("║ [1] View Passwords                        ║");
			Console.WriteLine("║ [2] Add Password                          ║");
			Console.WriteLine("║ [3] Search                                ║");
			Console.WriteLine("║ [4] Delete Password                       ║");
			Console.WriteLine("║ [5] Change Master Password                ║");
			//Console.WriteLine("║ [6] Sync                                  ║");
			Console.WriteLine("║ [6] View JSON                             ║");
			Console.WriteLine("║ [7] About                                 ║");
			Console.WriteLine("║ [ESC] Exit                                ║");
			Console.WriteLine("╚═══════════════════════════════════════════╝");
			Console.WriteLine();
		}

		public static void ShowAbout()
		{
			Console.Clear();
			UI.PrintHeader("ABOUT PROGRAM", false);
			Console.WriteLine("╠═══════════════════════════════════════════╣");
			Console.WriteLine($"║ Version: {Program.Version, -33}║");
			Console.WriteLine("║ Author: Lonewolf239                       ║");
			Console.WriteLine("║ GitHub: https://github.com/Lonewolf239    ║");
			Console.WriteLine("║                                           ║");
			Console.WriteLine("║ A secure application for managing your    ║");
			Console.WriteLine("║ passwords with encryption support.        ║");
			Console.WriteLine("║                                           ║");
			Console.WriteLine("║ Features:                                 ║");
			Console.WriteLine("║ • View, add, delete passwords             ║");
			Console.WriteLine("║ • Synchronize password databases          ║");
			Console.WriteLine("║ • Search functionality                    ║");
			Console.WriteLine("║ • Master password protection              ║");
			Console.WriteLine("║ • Secure encryption                       ║");
			Console.WriteLine("╚═══════════════════════════════════════════╝");
			Console.WriteLine();
			UI.PauseMenu();
		}

		public static void DisplayPasswordEntry(PasswordData pwd, int index)
		{
			Console.WriteLine($"┌─ {Colors.Bold}[{index}]{Colors.Reset} {pwd.PasswordType}");
			Console.WriteLine($"├─ Username: {pwd.Username}");
			Console.WriteLine($"├─ Password: {pwd.Password}");
			Console.WriteLine("└─" + new string('─', 43));
			Console.WriteLine();
		}

		public static string GetPassword(bool hide = true)
		{
			Console.CursorVisible = true;
			StringBuilder password = new();
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Enter && password.Length >= 8) break;
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

		public static string GetLine()
		{
			Console.CursorVisible = true;
			string line = Console.ReadLine() ?? "";
			Console.CursorVisible = false;
			return line;
		}

		public static (string service, string username, string password) GetPasswordInput()
		{
			Console.Write("  Enter service: ");
			string? service = GetLine();
			Console.Write("  Enter username: ");
			string? username = GetLine();
			Console.Write("  Enter password: ");
			string password = GetLine();
			Console.WriteLine();
			return (service ?? string.Empty, username ?? string.Empty, password);
		}

		public static bool ValidatePasswordInput(string service, string username, string password)
		{
			if (string.IsNullOrWhiteSpace(service))
			{
				PrintError("The service cannot be empty!");
				return false;
			}
			if (string.IsNullOrWhiteSpace(username))
			{
				PrintError("Username cannot be empty!");
				return false;
			}
			if (string.IsNullOrWhiteSpace(password))
			{
				PrintError("Password cannot be empty!");
				return false;
			}
			return true;
		}

		public static bool ConfirmAction(string message)
		{
			Console.Write($"{message} (y/n): ");
			bool result = false;
			while (true)
			{
				var key = Console.ReadKey(true).Key;
				if (key == ConsoleKey.Y)
				{
					result = true;
					break;
				}
				if (key == ConsoleKey.N) break;
			}
			Console.WriteLine();
			return result;
		}

		public static void PrintHeader(string title, bool showBottom = true)
		{
			int padding = (43 - title.Length) / 2;
			Console.WriteLine("╔═══════════════════════════════════════════╗");
			Console.WriteLine($"║{new string(' ', padding)}{Colors.Bold}{title}{Colors.Reset}{new string(' ', 43 - padding - title.Length)}║");
			if (showBottom)
			{
				Console.WriteLine("╚═══════════════════════════════════════════╝");
				Console.WriteLine();
			}
		}

		public static void PrintSuccess(string message) => Console.WriteLine($"{Colors.Success}✓ {message}{Colors.Reset}");

		public static void PrintError(string message) => Console.WriteLine($"{Colors.Error}✗ {message}{Colors.Reset}");

		public static void PrintWarning(string message) => Console.WriteLine($"{Colors.Warning}⚠ {message}{Colors.Reset}");

		public static void PrintInfo(string message) => Console.WriteLine($"{Colors.Info}ℹ {message}{Colors.Reset}");

		public static void PauseMenu()
		{
			PrintInfo("Press any key to continue...");
			Console.ReadKey(true);
		}

		public static void PrintColorizedLine(string line)
		{
			int i = 0;
			while (i < line.Length)
			{
				if (line[i] == ' ')
				{
					Console.Write(' ');
					i++;
				}
				else if (line[i] == '"')
				{
					int end = i + 1;
					while (end < line.Length && (line[end] != '"' || line[end - 1] == '\\')) end++;
					string stringValue = line.Substring(i, end - i + 1);
					if (line.Substring(end + 1).TrimStart().StartsWith(":")) Console.ForegroundColor = ConsoleColor.Cyan;
					else Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(stringValue);
					Console.ResetColor();
					i = end + 1;
				}
				else if (char.IsDigit(line[i]) || line[i] == '-')
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '-' || line[i] == '.'))
					{
						Console.Write(line[i]);
						i++;
					}
					Console.ResetColor();
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write(line[i]);
					Console.ResetColor();
					i++;
				}
			}
		}
	}
}
