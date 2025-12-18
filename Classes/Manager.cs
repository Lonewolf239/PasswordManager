using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace PasswordManager.Classes
{
	public class Manager
	{
		private readonly Passwords PasswordsManager;
		private bool ShouldExit = false;

		public Manager(Passwords passwords) => PasswordsManager = passwords;

		public void MainMenu()
		{
			while (!ShouldExit)
			{
				try
				{
					UI.DisplayMainMenu();
					ProcessMenuInput();
				}
				catch (Exception ex)
				{
					UI.PrintError($"Unexpected error: {ex.Message}");
					UI.PauseMenu();
				}
			}
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
				//case ConsoleKey.D6 or ConsoleKey.NumPad6:
				//	SyncPasswords();
				//	break;
				case ConsoleKey.D6 or ConsoleKey.NumPad6:
					ViewJson();
					break;
				case ConsoleKey.D7 or ConsoleKey.NumPad7:
					UI.ShowAbout();
					break;
				case ConsoleKey.Escape:
					ShouldExit = true;
					break;
			}
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
				UI.DisplayPasswordEntry(pwd, i + 1);
			}
			UI.PauseMenu();
		}

		private void ViewJson()
		{
			Console.Clear();
			UI.PrintHeader("VIEW JSON");
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

		private void AppendPassword()
		{
			Console.Clear();
			UI.PrintHeader("ADDING A PASSWORD");
			try
			{
				var (service, username, password) = UI.GetPasswordInput();
				if (!UI.ValidatePasswordInput(service, username, password)) return;
				if (PasswordExists(service, username))
				{
					UI.PrintWarning("The password for this service and user already exists!");
					if (!UI.ConfirmAction("Do you want to reset this password?"))
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
				if (!UI.ConfirmAction("Are you sure you want to delete these passwords?"))
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
				if (!UI.ConfirmAction("Do you want to continue?"))
				{
					UI.PrintInfo("The operation has been cancelled.");
					UI.PauseMenu();
					return;
				}
				Console.Write("  Enter the new master password: ");
				string newMaster = UI.GetPassword();
				Console.Write("  Repeat master password: ");
				string confirmMaster = UI.GetPassword();
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
				for (int i = 0; i < results.Count; i++) UI.DisplayPasswordEntry(results[i], i + 1);
				UI.PauseMenu();
			}
			catch (Exception ex)
			{
				UI.PrintError($"Error while searching: {ex.Message}");
				UI.PauseMenu();
			}
		}

		private bool PasswordExists(string service, string username) =>
			PasswordsManager.PasswordsList.Any(p => p.PasswordType == service && p.Username == username);
		
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
