namespace PasswordManager.Classes
{
    public class FileManager
    {
        private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "passwords.enc");

        public static void Save(string data, string path = "")
        {
            string filePath = path == "" ? FilePath : path;
            try
            {
                string encrypted = Crypto.Encrypt(data);
                File.WriteAllText(filePath, encrypted);
            }
            catch { }
        }

        public static string Read(string path = "")
        {
            string filePath = path == "" ? FilePath : path;
            try
            {
                if (!File.Exists(filePath)) return "[]";
                string encrypted = File.ReadAllText(filePath);
                string decrypted = Crypto.Decrypt(encrypted);
                return decrypted;
            }
            catch { return "[]"; }
        }
    }
}
