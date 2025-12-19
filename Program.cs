using System.Runtime.InteropServices;
using PasswordManager.Classes;

namespace PasswordManager
{
    public static class Program
    {
        public static string Version = "1.1";
        private static long LastActivityTicks = DateTime.Now.Ticks;
        private const int SessionTimeoutSeconds = 900;

        private static void OnApplicationClosing(Passwords passwords)
        {
            UI.Clear();
            Console.CursorVisible = true;
            FileManager.Save(passwords.ToString());
        }

        private static void OnKeyPressed() => Interlocked.Exchange(ref LastActivityTicks, DateTime.Now.Ticks);

        private static async Task Main()
        {
            UI.Clear();
            Console.CursorVisible = false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                UI.PrintError("This application is only supported on Linux.");
                UI.Pause();
                return;
            }
            await UpdateManager.CheckAndUpdate();
            string data = FileManager.Read();
            Passwords passwords = new();
            bool firstStart = false;
            if (data == "[]") firstStart = true;
            else passwords = new(data);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationClosing(passwords);
            while (true)
            {
                UI.LogIn(passwords, firstStart);
                firstStart = false;
                OnKeyPressed();
                using (var sessionCts = new CancellationTokenSource())
                {
                    _ = Task.Run(async () =>
                    {
                        while (!sessionCts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(1000, sessionCts.Token);
                            long lastTicks = Interlocked.Read(ref LastActivityTicks);
                            if ((DateTime.Now - new DateTime(lastTicks)).TotalSeconds >= SessionTimeoutSeconds)
                            {
                                sessionCts.Cancel();
                                break;
                            }
                        }
                    });
                    Manager manager = new(passwords, sessionCts.Token);
                    manager.KeyPressed += OnKeyPressed;
                    if (manager.MainMenu())
                    {
                        sessionCts.Cancel();
                        break;
                    }
                }
            }
        }
    }
}
