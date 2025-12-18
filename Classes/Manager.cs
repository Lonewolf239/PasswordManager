using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace PasswordManager.Classes
{
	public class Manager
	{
		private readonly Passwords PasswordsManager;
		private readonly MenuState MenuState = new();

		public Manager(Passwords passwords) => PasswordsManager = passwords;

		public void MainMenu()
		{
			while (!MenuState.ShouldExit)
			{
				try
				{
					DisplayMainMenu();
					ProcessMenuInput();
				}
				catch (Exception ex)
				{
					UI.PrintError($"Unexpected error: {ex.Message}");
					UI.PauseMenu();
				}
			}
		}

		private void DisplayMainMenu()
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
			Console.WriteLine("║ [6] Sync                                  ║");
			Console.WriteLine("║ [7] View JSON                             ║");
			Console.WriteLine("║ [8] About                                 ║");
			Console.WriteLine("║ [ESC] Exit                                ║");
			Console.WriteLine("╚═══════════════════════════════════════════╝");
			Console.WriteLine();
		}

		private void ProcessMenuInput()
		{
			switch (Console.ReadKey(true).Key)
			{
				case ConsoleKey.D1 or ConsoleKey.NumPad1:
					ViewPasswords();
					break;
				case ConsoleKey.D2 or ConsoleKey.NumPad2:
					AppendPassword();
					break;
				case ConsoleKey.D3 or ConsoleKey.NumPad3:
					SearchPassword();
					break;
				case ConsoleKey.D4 or ConsoleKey.NumPad4:
					RemovePassword();
					break;
				case ConsoleKey.D5 or ConsoleKey.NumPad5:
					ChangeMasterPassword();
					break;
				case ConsoleKey.D6 or ConsoleKey.NumPad6:
					SyncPasswords();
					break;
				case ConsoleKey.D7 or ConsoleKey.NumPad7:
					ViewJson();
					break;
				case ConsoleKey.D8 or ConsoleKey.NumPad8:
					ShowAbout();
					break;
				case ConsoleKey.Escape:
					MenuState.ShouldExit = true;
					break;
			}
		}

		private void ShowAbout()
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

		private void ViewPasswords()
		{
			Console.Clear();
			UI.PrintHeader("VIEW PASSWORDS");
			if (PasswordsManager.PasswordsList.Count == 0)
			{
				UI.PrintWarning("No passwords found.");
				UI.PauseMenu();
				return;
			}
			for (int i = 0; i < PasswordsManager.PasswordsList.Count; i++)
			{
				var pwd = PasswordsManager.PasswordsList[i];
				DisplayPasswordEntry(pwd, i + 1);
			}
			UI.PauseMenu();
		}

		private void ViewJson()
		{
			Console.Clear();
			UI.PrintHeader("VIEW JSON");
			if (PasswordsManager.PasswordsList.Count == 0)
			{
				UI.PrintWarning("No passwords found.");
				UI.PauseMenu();
				return;
			}
			string json = PasswordsManager.ToString();
			var lines = json.Split('\n');
			foreach (var line in lines)
			{
				UI.PrintColorizedLine(line);
				Console.WriteLine();
			}
			Console.WriteLine();
			UI.PauseMenu();
		}

		private void DisplayPasswordEntry(PasswordData pwd, int index)
		{
			Console.WriteLine($"┌─ {Colors.Bold}[{index}]{Colors.Reset} {pwd.PasswordType}");
			Console.WriteLine($"├─ Username: {pwd.Username}");
			Console.WriteLine($"├─ Password: {pwd.Password}");
			Console.WriteLine("└─" + new string('─', 43));
			Console.WriteLine();
		}

		private void AppendPassword()
		{
			Console.Clear();
			UI.PrintHeader("ADDING A PASSWORD");
			try
			{
				var (service, username, password) = GetPasswordInput();
				if (!ValidatePasswordInput(service, username, password)) return;
				if (PasswordExists(service, username))
				{
					UI.PrintWarning("The password for this service and user already exists!");
					if (!ConfirmAction("Do you want to reset this password?"))
					{
						UI.PrintInfo("The operation has been cancelled.");
						UI.PauseMenu();
						return;
					}
					PasswordsManager.RemoveExactMatch(service, username);
				}
				var newPwd = new PasswordData(service, username, password);
				PasswordsManager.Append(newPwd);
				SaveAndNotify("Password added successfully!");
				UI.PauseMenu();
			}
			catch (Exception ex)
			{
				UI.PrintError($"Error adding password: {ex.Message}");
				UI.PauseMenu();
			}
		}

		private void SyncPasswords()
		{
			Console.Clear();
			UI.PrintHeader("PASSWORD SYNCHRONIZATION");
			try
			{
				Console.Write("Enter the path to the files: ");
				string path = UI.GetLine();
				if (string.IsNullOrWhiteSpace(path))
				{
					UI.PrintError("The path cannot be empty!");
					UI.PauseMenu();
					return;
				}
				if (!File.Exists(path) && !Directory.Exists(path))
				{
					UI.PrintError($"Path not found: {path}");
					UI.PauseMenu();
					return;
				}
				string data = FileManager.Read(path);
				if (data == "[]")
				{
					UI.PrintError("No data to sync!");
					UI.PauseMenu();
					return;
				}
				PasswordsManager.Sync(data);
				SaveAndNotify("Passwords have been successfully synchronized!");
				UI.PauseMenu();
			}
			catch (Exception ex)
			{
				UI.PrintError($"Error while synchronizing: {ex.Message}");
				UI.PauseMenu();
			}
		}

		private void RemovePassword()
		{
			Console.Clear();
			UI.PrintHeader("REMOVE PASSWORD");
			try
			{
				UI.PrintInfo("Search by criteria (leave blank to skip):");
				Console.Write("  Service: ");
				string? service = UI.GetLine();
				Console.Write("  Username: ");
				string? username = UI.GetLine();
				Console.WriteLine();
				var results = PasswordsManager.Find(service, username);
				if (results.Count == 0)
				{
					UI.PrintWarning("No passwords were found matching the criteria.");
					UI.PauseMenu();
					return;
				}
				UI.PrintSuccess($"Matches found: {results.Count}");
				Console.WriteLine();
				for (int i = 0; i < results.Count; i++)
				{
					var pwd = results[i];
					Console.WriteLine($"  [{i + 1}] {pwd.PasswordType} - {pwd.Username}");
				}
				Console.WriteLine();
				if (!ConfirmAction("Are you sure you want to delete these passwords?"))
				{
					UI.PrintInfo("The operation has been cancelled.");
					UI.PauseMenu();
					return;
				}
				foreach (var pwd in results) PasswordsManager.Remove(pwd);
				SaveAndNotify($"Passwords removed: {results.Count}");
				UI.PauseMenu();
			}
			catch (Exception ex)
			{
				UI.PrintError($"Error deleting password: {ex.Message}");
				UI.PauseMenu();
			}
		}

		private void ChangeMasterPassword()
		{
			Console.Clear();
			UI.PrintHeader("CHANGING THE MASTER PASSWORD");
			try
			{
				UI.PrintWarning("IMPORTANT: Losing your master password means losing access to all passwords!");
				Console.WriteLine();
				if (!ConfirmAction("Do you want to continue?"))
				{
					UI.PrintInfo("The operation has been cancelled.");
					UI.PauseMenu();
					return;
				}
				Console.Write("  Enter the new master password: ");
				string newMaster = Program.GetPassword();
				if (string.IsNullOrWhiteSpace(newMaster))
				{
					UI.PrintError("Password cannot be empty!");
					UI.PauseMenu();
					return;
				}
				Console.Write("  Repeat master password: ");
				string confirmMaster = Program.GetPassword();
				if (newMaster != confirmMaster)
				{
					UI.PrintError("Passwords don't match!");
					UI.PauseMenu();
					return;
				}
				PasswordsManager.ChangeMainPassword(newMaster);
				SaveAndNotify("Master password successfully changed!");
				UI.PauseMenu();
			}
			catch (Exception ex)
			{
				UI.PrintError($"Error changing password: {ex.Message}");
				UI.PauseMenu();
			}
		}

		private void SearchPassword()
		{
			Console.Clear();
			UI.PrintHeader("PASSWORD SEARCH");
			try
			{
				UI.PrintInfo("Enter your search details (leave blank to skip):");
				Console.Write("  Service: ");
				string? service = UI.GetLine();
				Console.Write("  Username: ");
				string? username = UI.GetLine();
				Console.WriteLine();
				if (string.IsNullOrWhiteSpace(service) && string.IsNullOrWhiteSpace(username))
				{
					UI.PrintWarning("Please enter at least one search criterion.");
					UI.PauseMenu();
					return;
				}
				var results = PasswordsManager.Find(service, username);
				if (results.Count == 0)
				{
					UI.PrintWarning("Nothing found matching your request.");
					UI.PauseMenu();
					return;
				}
				UI.PrintSuccess($"Results found: {results.Count}");
				Console.WriteLine();
				for (int i = 0; i < results.Count; i++) DisplayPasswordEntry(results[i], i + 1);
				UI.PauseMenu();
			}
			catch (Exception ex)
			{
				UI.PrintError($"Error while searching: {ex.Message}");
				UI.PauseMenu();
			}
		}

		private (string service, string username, string password) GetPasswordInput()
		{
			Console.Write("  Enter service: ");
			string? service = UI.GetLine();
			Console.Write("  Enter username: ");
			string? username = UI.GetLine();
			Console.Write("  Enter password: ");
			string password = UI.GetLine();
			Console.WriteLine();
			return (service ?? string.Empty, username ?? string.Empty, password);
		}

		private bool ValidatePasswordInput(string service, string username, string password)
		{
			if (string.IsNullOrWhiteSpace(service))
			{
				UI.PrintError("The service cannot be empty!");
				return false;
			}
			if (string.IsNullOrWhiteSpace(username))
			{
				UI.PrintError("Username cannot be empty!");
				return false;
			}
			if (string.IsNullOrWhiteSpace(password))
			{
				UI.PrintError("Password cannot be empty!");
				return false;
			}
			return true;
		}

		private bool PasswordExists(string service, string username) =>
			PasswordsManager.PasswordsList.Any(p => p.PasswordType == service && p.Username == username);

		private bool ConfirmAction(string message)
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
		
		private void SaveAndNotify(string message)
		{
			try
			{
				FileManager.Save(PasswordsManager.ToString());
				UI.PrintSuccess(message);
			}
			catch (Exception ex) { UI.PrintError($"Error saving: {ex.Message}"); }
		}
	}
 }
