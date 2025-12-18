using System.Text.Json;
using System.Collections.Generic;

namespace PasswordManager.Classes
{
	public class PasswordsDto
	{
		public string mainPassword { get; set; } = "";
		public Dictionary<string, List<PasswordDto>> passwords { get; set; } = [];
	}

	public class PasswordDto
	{
		public string username { get; set; } = "";
		public string password { get; set; } = "";
	}

	public class Passwords
	{
		public string MainPassword { get; private set; } = "";
		public List<PasswordData> PasswordsList { get; private set; }

		public Passwords() => PasswordsList = [];

		public Passwords(string input)
		{
			PasswordsList = [];
			try
			{
				var dto = JsonSerializer.Deserialize<PasswordsDto>(input);
				if (dto != null)
				{
					MainPassword = dto.mainPassword;
					if (dto.passwords != null)
					{
						foreach (var typeGroup in dto.passwords)
						{
							string type = typeGroup.Key;
							foreach (var p in typeGroup.Value)
								PasswordsList.Add(new PasswordData(type, p.username, p.password));
						}
					}
				}
			}
			catch {}
		}

		public override string ToString()
		{
			var passwordsByType = new Dictionary<string, List<PasswordDto>>();
			foreach (var p in PasswordsList)
			{
				if (!passwordsByType.ContainsKey(p.PasswordType)) passwordsByType[p.PasswordType] = [];
				passwordsByType[p.PasswordType].Add(new PasswordDto { username = p.Username, password = p.Password });
			}
			var obj = new PasswordsDto
			{
				mainPassword = MainPassword,
				passwords = passwordsByType
			};
			return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
		}

		public void Append(PasswordData password)
		{
			if (PasswordsList.Contains(password)) return;
			PasswordsList.Add(password);
		}

		public void RemoveExactMatch(string type, string username)
		{
			foreach (var item in PasswordsList)
			{
				if (item.PasswordType == type && item.Username == username)
				{
					PasswordsList.Remove(item);
					return;
				}
			}
		}
		
		public void Remove(string type = "", string username = "")
		{
			var passwords = Find(type, username);
			foreach (var item in passwords) Remove(item);
		}

		public void Remove(PasswordData password) => PasswordsList.Remove(password);

		public void ChangeMainPassword(string password) => MainPassword = Hasher.Get(password);

		public void Sync(string json)
		{
			try
			{
				var dto = JsonSerializer.Deserialize<PasswordsDto>(json);
				if (dto != null && dto.passwords != null)
				{
					foreach (var typeGroup in dto.passwords)
					{
						string type = typeGroup.Key;
						foreach (var p in typeGroup.Value)
						{
							bool exists = false;
							foreach (var existing in PasswordsList)
							{
								if (existing.PasswordType == type &&
										existing.Username == p.username &&
										existing.Password == p.password)
								{
									exists = true;
									break;
								}
							}
							if (!exists) PasswordsList.Add(new PasswordData(type, p.username, p.password));
						}
					}
				}
			}
			catch {}
		}

		public List<PasswordData> GetByType(string type) => Find(type: type);

		public List<PasswordData> GetByUsername(string username) => Find(username: username);

		public List<PasswordData> Find(string? type = null, string? username = null)
		{
			List<PasswordData> result = [];
			foreach (var item in PasswordsList)
			{
				bool typeMatch = string.IsNullOrEmpty(type) || item.PasswordType.Contains(type, StringComparison.OrdinalIgnoreCase);
				bool usernameMatch = string.IsNullOrEmpty(username) || item.Username.Contains(username, StringComparison.OrdinalIgnoreCase);
				if (typeMatch && usernameMatch)
					result.Add(item);
			}
			return result;
		}

		public bool CheckMainPassword(string password)
		{
			if (string.IsNullOrEmpty(MainPassword)) return true;
			return Hasher.Verify(password, MainPassword);
		}

		public void Clear(string password)
		{
			MainPassword = Hasher.Get(password);
			PasswordsList.Clear();
		}
	}

	public class PasswordData
	{
		public string PasswordType { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		public PasswordData(string passwordType, string username, string password)
		{
			PasswordType = passwordType;
			Username = username;
			Password = password;
		}
	}
}
