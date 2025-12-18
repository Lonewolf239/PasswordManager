using System;
using System.Text;
using System.Security.Cryptography;

namespace PasswordManager
{
	public class Hasher
	{
		public static string Get(string input)
		{
			using (var sha256 = SHA256.Create())
			{
				byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
				return Convert.ToBase64String(hashedBytes);
			}
		}
		
		public static bool Verify(string input, string hash)
		{
			string hashOfInput = Get(input);
			return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hashOfInput), Encoding.UTF8.GetBytes(hash));
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

        public class MenuState
        {
            public int CurrentAction { get; set; }
            public bool ShouldExit { get; set; }
            public string LastMessage { get; set; } = string.Empty;
	}

	public class UI
	{
		public static string GetLine()
		{
			Console.CursorVisible = true;
			string line = Console.ReadLine() ?? "";
			Console.CursorVisible = false;
			return line;
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
