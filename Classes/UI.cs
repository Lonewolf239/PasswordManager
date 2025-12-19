using System.Text;

namespace PasswordManager.Classes
{
    public static class UI
    {
        public static void DisplayMainMenu()
        {
            Clear();
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
            Clear();
            UI.PrintHeader("ABOUT PROGRAM", false);
            Console.WriteLine("╠═══════════════════════════════════════════╣");
            Console.WriteLine($"║ Version: {Program.Version,-33}║");
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
            UI.Pause();
        }

        public static void DisplayPasswordEntry(PasswordData pwd, int index)
        {
            Console.WriteLine($"┌─ {Colors.Bold}[{index}]{Colors.Reset} {pwd.PasswordType}");
            Console.WriteLine($"├─ Username: {pwd.Username}");
            Console.WriteLine($"├─ Password: {pwd.Password}");
            Console.WriteLine("└─" + new string('─', 43));
            Console.WriteLine();
        }

        public static string GetLine(string message, bool hide = false, int minLength = 0, int maxLength = -1)
        {
            Console.CursorVisible = true;
            StringBuilder password = new();
            Console.Write(message + ' ');
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter && password.Length >= minLength) break;
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
                    if (maxLength != -1 && password.Length == maxLength)
                        continue;
                    password.Append(key.KeyChar);
                    if (hide) Console.Write('*');
                    else Console.Write(key.KeyChar);
                }
            }
            Console.WriteLine();
            Console.CursorVisible = false;
            return password.ToString();
        }

        public static void LogIn(Passwords passwords, bool firstStart)
        {
            int attemps = 0;
            while (true)
            {
                Clear();
                string password = "";
                if (firstStart)
                {
                    password = GetPassword();
                    passwords.ChangeMainPassword(password);
                    FileManager.Save(passwords.ToString());
                    break;
                }
                else password = GetLine("Enter password:", true, 8, 24);
                if (!passwords.CheckMainPassword(password))
                {
                    PrintError("Incorrect password!");
                    attemps++;
                    if (attemps == 3)
                    {
                        if (ConfirmAction("\nWARNING: This action will erase all data.\nDo you want to restore access?"))
                        {
                            password = GetPassword();
                            passwords.Clear(password);
                            FileManager.Save(passwords.ToString());
                            break;
                        }
                        attemps = 0;
                    }
                    else Thread.Sleep(500);
                }
                else break;
            }
        }

        public static string GetPassword(string firstMessage = "Enter new password:", string secondMessage = "Confirm password:")
        {
            while (true)
            {
                Clear();
                string firstPassword = GetLine(firstMessage, true, 8, 24);
                string secondPassword = GetLine(secondMessage, true, 8, 24);
                if (firstPassword != secondPassword)
                {
                    PrintError("Passwords don't match!");
                    Thread.Sleep(500);
                }
                else return firstPassword;
            }
        }

        public static (string service, string username, string password) GetPasswordInput()
        {
            string service = GetLine("  Enter service:");
            string username = GetLine("  Enter username:");
            string password = GetLine("  Enter password:");
            Console.WriteLine();
            return (service, username, password);
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
            Console.Write($"{message} [Y/N]:");
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

        public static void Pause()
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

        public static void Clear() => Console.Write("\x1b[2J\x1b[3J\x1b[H");
    }
}
