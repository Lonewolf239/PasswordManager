# Password Manager

A secure password manager with AES-256 encryption.

**Version:** 1.1
**Platform:** Linux

---

## Requirements

- .NET Runtime 8.0+
- Linux
- Home directory permissions

---

## Installation

```bash
dotnet build -c Release
dotnet run
```

Or:

```bash
./PasswordManager
```

---

## Features

- View passwords
- Add passwords
- Search
- Delete passwords
- Change master password
- View JSON
- Check for updates

---

## File structure

| File | Purpose |
|------|---------|
| Program.cs | Entry Point and Authentication |
| Manager.cs | Main Menu and Commands |
| Passwords.cs | Password and JSON Management |
| FileManager.cs | Encryption and Saving |
| UpdateManager.cs | Checking for Updates |
| Utils.cs | Utilities and UI |

---

## Security

- **Passwords:** AES-256-CBC
- **Master Password:** SHA-256
- **Files:** passwords.enc, secret.key
- **Security:** Limit Attempts (3), FixedTimeEquals

---

## Updates

Checked automatically at startup, saving a backup copy.
