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
				string password = UI.GetPassword(!firstStart);
				if (firstStart)
				{
					passwords.ChangeMainPassword(password);
					FileManager.Save(passwords.ToString());
					break;
				}
				if (!passwords.CheckMainPassword(password))
				{
					Console.WriteLine("Incorrect password!");
					attemps++;
					if (attemps == 3)
					{
						if (UI.ConfirmAction("\nWARNING: This action will erase all data.\nDo you want to restore access?"))
						{
							Console.Write("Enter new password: ");
							password = UI.GetPassword(false);
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
			AppDomain.CurrentDomain.ProcessExit += (s, e) => OnApplicationClosing(passwords);
			Manager manager = new(passwords);
			manager.MainMenu();
		}
	}
}
