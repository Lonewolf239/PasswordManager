using System.Security.Cryptography;

namespace PasswordManager.Classes
{
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
}
