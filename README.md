# C# .Net-Core 10 FTPS Server and Library

A .Net-Core 10 FTPS Server implementation. Has nuget Library (cross-platform, no dependencies, open-source permissive license), Console (Linux, Windows), UI (Windows x64 and ARM64, Ubuntu 24).

Features:

- ✅ **JSON File Configuration**
- ✅ **Command-Line Arguments** - Override any setting via CLI
- ✅ **User Permissions** - Granular control over Read/Write operations
- ✅ **Per-User Root Folders** - Isolated directories for each user
- ✅ **TLS/SSL Encryption** - Secure FTPS connections
- ✅ **Full FTP Protocol** - All standard FTP commands supported
- ✅ **Path Security** - Protection against directory traversal attacks


| Component                      |                                                                                   |
|--------------------------------------------------------------------------------------------------------------------|
| [Library](./README_NUGET.md)   | [NUGet Package](https://www.nuget.org/packages/Siarhei_Kuchuk.FtpsServerLibrary)  |
| [Console](./README_CONSOLE.md) |                                                                                   |

## Applications

### Common

Both tools can be obtained from Releases section. You can get then either in setup or binaries.

[See Ubuntu](./Ubuntu.md)

**Log Locations:**

Location: `%AppData%/ftps-server/logs`

**Self-Signed Certificate Location**

If certificate is not specified, self-signed certificate will be created and stored in %localappdata%\FtpsServerLibrary\Certificates.

### **UI** (available for **Windowx x64 or ARM64** in **Installer**, **Binaries**, for **Ubuntu 24** of any architecture in **Installation Script**)

<img width="1109" height="614" alt="image" src="https://github.com/user-attachments/assets/da502ae9-01ae-4bfe-9619-653d6395067b" />

## 🎯 Use Cases

- 1. Exchange of files between PC and notebook over WI-FI.
- 2. Access to PC files from Android file manager over WI-FI.

## Connecting to the Server

### Using FileZilla

1. Open FileZilla
2. Go to **File → Site Manager**
3. Click **New Site**
4. Configure:
   - **Protocol**: FTP - File Transfer Protocol
   - **Host**: 127.0.0.1 (or your server IP)
   - **Port**: 2121 (or your configured port)
   - **Encryption**: Require explicit FTP over TLS
   - **Logon Type**: Normal
   - **User**: admin (or your username)
   - **Password**: password123 (or your password)
5. Click **Connect**

### Using WinSCP (Windows)

1. New Site
2. File protocol: **FTP**
3. Encryption: **TLS/SSL Explicit encryption**
4. Host name: **127.0.0.1**
5. Port number: **2121**
6. User name: **admin** (or your username)
7. Password: **password123** (or your password)
8. Click **Login**

## Troubleshooting

## 🐛 Troubleshooting

**"Port already in use"**
- Change port in `appsettings.json` or use `--port` flag

**"Certificate not found"**
- Run certificate generation (option 4 in startup scripts)

**"Permission denied" when starting**
- Windows: Run as Administrator for ports < 1024
- Linux: Use `sudo` or port > 1024

**Client can't connect**
- Check firewall settings
- Verify server is running: `netstat -an | grep 2121`
- Try telnet test: `telnet 0.0.0.0 2121`

