# FTPS Server - Quick Start Guide

## 🚀 Quick Setup (5 minutes)

### Windows

1. **Extract all files** to a folder (e.g., `C:\FtpsServer\`)

2. **Open PowerShell as Administrator** and run:
   ```powershell
   New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
   ```

3. **Double-click** `start-server.bat` and choose option 1

4. **Connect** with FileZilla:
   - Host: `127.0.0.1`
   - Port: `21990`
   - User: `admin`
   - Password: `password123`
   - Encryption: Require explicit FTP over TLS

### Linux/macOS

1. **Extract all files** to a folder

2. **Open terminal** in that folder and run:
   ```bash
   chmod +x start-server.sh
   ./start-server.sh
   ```
   Choose option 4 to generate certificate, then option 1 to start

3. **Connect** with an FTP client:
   ```bash
   lftp -u admin,password123 -e "set ftp:ssl-force true; set ssl:verify-certificate no" 127.0.0.1:21990
   ```

## 📋 Files Included

| File | Description |
|------|-------------|
| `Program.cs` | Main server code |
| `FtpsServer.csproj` | Project file with NLog dependency |
| `NLog.config` | Logging configuration |
| `appsettings.json` | Server configuration (users, ports, etc.) |
| `start-server.bat` | Windows startup script |
| `start-server.sh` | Linux/macOS startup script |
| `README_Enhanced.md` | Complete documentation |
| `QUICKSTART.md` | This file |

## ⚙️ Default Configuration

**Server:**
- IP: `127.0.0.1` (localhost)
- Port: `21990`
- Root: `./ftproot` (created automatically)

**Users:**
| Username | Password | Folder | Permissions |
|----------|----------|--------|-------------|
| admin | password123 | / | Full access |
| readonly | readonly123 | /public | Read only |
| user | user123 | /users/user | Full access |

## 🔧 Customization

### Quick Config Changes

Edit `appsettings.json` to change:
- Port number
- User accounts
- Permissions
- Root directories

### Command-Line Override

```bash
# Windows
dotnet run -- --port 2121 --user myuser:mypass:/home:RWDCXN

# Linux/macOS
./start-server.sh
# Then choose option 3 for command-line setup
```

## 📊 Monitoring

**View logs:**
- Location: `%AppData%/ftps-server/logs/ftps-YYYY-MM-DD.log`
- Real-time: Console shows colored output

**Check connections:**
- Console shows: `Client connected: IP:PORT (Active: N)`

## 🔒 Security Notes

⚠️ **Important for Production:**
1. Change default passwords immediately
2. Use a proper CA-signed certificate
3. Don't expose to the internet without firewall rules
4. Review user permissions carefully
5. Monitor logs regularly

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
- Verify server is running: `netstat -an | grep 21990`
- Try telnet test: `telnet 127.0.0.1 21990`

## 📚 Full Documentation

See `README.md` for:
- Complete command-line options
- Advanced configuration
- User permission system
- Production deployment guide
- Security best practices

## 🎯 Common Use Cases

### 1. Public Download Server (Read-Only)
```json
{
  "Username": "guest",
  "Password": "guest",
  "RootFolder": "/public",
  "Permissions": {
    "Read": true,
    "Write": false
  }
}
```

### 2. File Upload Drop Box
```json
{
  "Username": "uploader",
  "Password": "upload123",
  "RootFolder": "/uploads",
  "Permissions": {
    "Read": false,
    "Write": true
  }
}
```

### 3. Personal User Workspace
```json
{
  "Username": "john",
  "Password": "john123",
  "RootFolder": "/users/john",
  "Permissions": {
    "Read": true,
    "Write": true
  }
}
```

## 💡 Tips

- **Testing:** Always test locally before exposing to network
- **Logging:** Set `minlevel="Debug"` in NLog.config for troubleshooting
- **Backup:** Keep a backup of `appsettings.json` and certificates
- **Updates:** Check for .NET security updates regularly

## 🆘 Getting Help

1. Check console output for errors
2. Review logs in `logs/` folder
3. Read `README_Enhanced.md` for details
4. Verify configuration in `appsettings.json`

## 🔑 Permission Flags Quick Reference

When using command-line `--user` option:

| Flag | Permission | FTP Commands |
|------|------------|--------------|
| R | Read | LIST, RETR, SIZE |
| W | Write | STOR (upload) |
| D | Delete | DELE (delete files) |
| C | Create Dirs | MKD (make directory) |
| X | Delete Dirs | RMD (remove directory) |
| N | Rename | RNFR/RNTO (rename) |

**Examples:**
- `RW` = Full access (all permissions)
- `R` = Read-only (download and list only)
- `W` = Write-only (upload only, can't see files)

## 🎨 Example Commands

**Start with custom port:**
```bash
dotnet run -- --port 2121
```

**Add multiple users:**
```bash
dotnet run -- --user admin:pass1:/admin:RWDCXN --user guest:pass2:/public:R
```

**Use custom certificate:**
```bash
dotnet run -- --cert mycert.pfx --certpass mypassword
```

**Bind to all interfaces:**
```bash
dotnet run -- --ip 0.0.0.0
```

---

**Need more help?** See `README.md` for comprehensive documentation!
