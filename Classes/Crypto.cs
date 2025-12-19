using System.Security.Cryptography;

namespace PasswordManager.Classes
{
    public static class Crypto
    {
        private static readonly string KeyPath = Path.Combine(AppContext.BaseDirectory, "secret.key");
        private static readonly Lazy<byte[]> _Key = new(InitKey);
        private static byte[] Key => _Key.Value;

        private static byte[] InitKey()
        {
            if (File.Exists(KeyPath)) return File.ReadAllBytes(KeyPath);
            var newKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(newKey);
            File.WriteAllBytes(KeyPath, newKey);
            try { File.SetUnixFileMode(KeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch { }
            return newKey;
        }

        public static string Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
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

        public static string Decrypt(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
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
}
