using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace PasswordManager.Classes
{
	public class FileManager
	{
		private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "passwords.enc");
		private static readonly string KeyPath = Path.Combine(AppContext.BaseDirectory, "secret.key");
		private static readonly Lazy<byte[]> _Key = new Lazy<byte[]>(InitKey);
		private static byte[] Key => _Key.Value;

		private static byte[] InitKey()
		{
			if (File.Exists(KeyPath)) return File.ReadAllBytes(KeyPath);
			var newKey = new byte[32];
			using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(newKey);
			File.WriteAllBytes(KeyPath, newKey);
			try { File.SetUnixFileMode(KeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
			catch {}
			return newKey;
		}

		public static void Save(string data, string path = "")
		{
			string filePath = path == "" ? FilePath : path;
			try
			{
				string encrypted = Crypto.Encrypt(data, Key);
				File.WriteAllText(filePath, encrypted);
			}
			catch {}
		}

		public static string Read(string path = "")
		{
			string filePath = path == "" ? FilePath : path;
			try
			{
				if (!File.Exists(filePath)) return "[]";
				string encrypted = File.ReadAllText(filePath);
				string decrypted = Crypto.Decrypt(encrypted, Key);
				return decrypted;
			}
			catch { return "[]"; }
		}
	}
}
