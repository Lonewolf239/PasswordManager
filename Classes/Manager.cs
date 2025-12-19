namespace PasswordManager.Classes
{
    public class Manager
    {
        public Action? KeyPressed;
        private readonly Passwords PasswordsManager;
        private readonly CancellationToken SessionToken;
        private bool ShouldExit = false;

        public Manager(Passwords passwords, CancellationToken token)
        {
            PasswordsManager = passwords;
            SessionToken = token;
        }

        public bool MainMenu()
        {
            while (!ShouldExit)
            {
                try
                {
                    if (SessionToken.IsCancellationRequested) return false;
                    UI.DisplayMainMenu();
                    if (!WaitForInputOrCancel()) return false;
                    ProcessMenuInput();
                    KeyPressed?.Invoke();
                }
                catch (Exception ex)
                {
                    UI.PrintError($"Unexpected error: {ex.Message}");
                    UI.Pause();
                }
            }
            return true;
        }

        private bool WaitForInputOrCancel()
        {
            while (!Console.KeyAvailable)
            {
                if (SessionToken.IsCancellationRequested) return false;
                Thread.Sleep(5);
            }
            KeyPressed?.Invoke();
            return true;
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
            UI.Clear();
            UI.PrintHeader("VIEW PASSWORDS");
            if (PasswordsManager.PasswordsList.Count == 0)
            {
                UI.PrintWarning("No passwords found.");
                UI.Pause();
                return;
            }
            for (int i = 0; i < PasswordsManager.PasswordsList.Count; i++)
            {
                var pwd = PasswordsManager.PasswordsList[i];
                UI.DisplayPasswordEntry(pwd, i + 1);
            }
            UI.Pause();
        }

        private void ViewJson()
        {
            UI.Clear();
            UI.PrintHeader("VIEW JSON");
            string json = PasswordsManager.ToString();
            var lines = json.Split('\n');
            foreach (var line in lines)
            {
                UI.PrintColorizedLine(line);
                Console.WriteLine();
            }
            Console.WriteLine();
            UI.Pause();
        }

        private void AppendPassword()
        {
            UI.Clear();
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
                        UI.Pause();
                        return;
                    }
                    PasswordsManager.RemoveExactMatch(service, username);
                }
                var newPwd = new PasswordData(service, username, password);
                PasswordsManager.Append(newPwd);
                SaveAndNotify("Password added successfully!");
                UI.Pause();
            }
            catch (Exception ex)
            {
                UI.PrintError($"Error adding password: {ex.Message}");
                UI.Pause();
            }
        }

        private void SyncPasswords()
        {
            UI.Clear();
            UI.PrintHeader("PASSWORD SYNCHRONIZATION");
            try
            {
                string path = UI.GetLine("Enter the path to the files:", minLength: 0);
                if (string.IsNullOrWhiteSpace(path))
                {
                    UI.PrintError("The path cannot be empty!");
                    UI.Pause();
                    return;
                }
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    UI.PrintError($"Path not found: {path}");
                    UI.Pause();
                    return;
                }
                string data = FileManager.Read(path);
                if (data == "[]")
                {
                    UI.PrintError("No data to sync!");
                    UI.Pause();
                    return;
                }
                PasswordsManager.Sync(data);
                SaveAndNotify("Passwords have been successfully synchronized!");
                UI.Pause();
            }
            catch (Exception ex)
            {
                UI.PrintError($"Error while synchronizing: {ex.Message}");
                UI.Pause();
            }
        }

        private void RemovePassword()
        {
            UI.Clear();
            UI.PrintHeader("REMOVE PASSWORD");
            try
            {
                UI.PrintInfo("Search by criteria (leave blank to skip):");
                string service = UI.GetLine("  Service:");
                string username = UI.GetLine("  Username:");
                Console.WriteLine();
                var results = PasswordsManager.Find(service, username);
                if (results.Count == 0)
                {
                    UI.PrintWarning("No passwords were found matching the criteria.");
                    UI.Pause();
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
                    UI.Pause();
                    return;
                }
                foreach (var pwd in results) PasswordsManager.Remove(pwd);
                SaveAndNotify($"Passwords removed: {results.Count}");
                UI.Pause();
            }
            catch (Exception ex)
            {
                UI.PrintError($"Error deleting password: {ex.Message}");
                UI.Pause();
            }
        }

        private void ChangeMasterPassword()
        {
            UI.Clear();
            UI.PrintHeader("CHANGING THE MASTER PASSWORD");
            try
            {
                UI.PrintWarning("IMPORTANT: Losing your master password means losing access to all passwords!");
                Console.WriteLine();
                if (!UI.ConfirmAction("Do you want to continue?"))
                {
                    UI.PrintInfo("The operation has been cancelled.");
                    UI.Pause();
                    return;
                }
                string newMaster = UI.GetPassword("  Enter the new master password:", "  Repeat master password:");
                PasswordsManager.ChangeMainPassword(newMaster);
                SaveAndNotify("Master password successfully changed!");
                UI.Pause();
            }
            catch (Exception ex)
            {
                UI.PrintError($"Error changing password: {ex.Message}");
                UI.Pause();
            }
        }

        private void SearchPassword()
        {
            UI.Clear();
            UI.PrintHeader("PASSWORD SEARCH");
            try
            {
                UI.PrintInfo("Enter your search details (leave blank to skip):");
                string service = UI.GetLine("  Service:");
                string username = UI.GetLine("  Username:");
                Console.WriteLine();
                if (string.IsNullOrWhiteSpace(service) && string.IsNullOrWhiteSpace(username))
                {
                    UI.PrintWarning("Please enter at least one search criterion.");
                    UI.Pause();
                    return;
                }
                var results = PasswordsManager.Find(service, username);
                if (results.Count == 0)
                {
                    UI.PrintWarning("Nothing found matching your request.");
                    UI.Pause();
                    return;
                }
                UI.PrintSuccess($"Results found: {results.Count}");
                Console.WriteLine();
                for (int i = 0; i < results.Count; i++) UI.DisplayPasswordEntry(results[i], i + 1);
                UI.Pause();
            }
            catch (Exception ex)
            {
                UI.PrintError($"Error while searching: {ex.Message}");
                UI.Pause();
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
