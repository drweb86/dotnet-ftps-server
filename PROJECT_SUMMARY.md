# FTPS Server Project - Complete Package

## 📦 What's Included

This is a complete, production-ready FTPS (FTP over TLS) server implementation in C# with the following features:

### Core Features
✅ **NLog Logging** - Industry-standard logging with file rotation and colored console output
✅ **JSON Configuration** - Easy configuration via appsettings.json
✅ **Command-Line Arguments** - Override any setting via CLI
✅ **User Permissions** - Granular control over Read/Write/Delete/Create/Rename operations
✅ **Per-User Root Folders** - Isolated directories for each user
✅ **TLS/SSL Encryption** - Secure FTPS connections
✅ **Full FTP Protocol** - All standard FTP commands supported
✅ **Path Security** - Protection against directory traversal attacks
✅ **Comprehensive Documentation** - Multiple guides for different user levels

## 📁 File Listing

### Core Application Files
| File | Description | Required |
|------|-------------|----------|
| `Program.cs` | Main server implementation | ✅ Yes |
| `FtpsServer.csproj` | .NET project file with dependencies | ✅ Yes |
| `NLog.config` | Logging configuration | ✅ Yes |
| `appsettings.json` | Server configuration | ⚠️ Optional* |

*If not provided, can use command-line arguments

### Documentation
| File | Description | Audience |
|------|-------------|----------|
| `QUICKSTART.md` | 5-minute setup guide | New users |
| `README_Enhanced.md` | Comprehensive documentation | All users |
| `README.md` | Original basic documentation | Reference |

### Utilities
| File | Description | Platform |
|------|-------------|----------|
| `start-server.bat` | Interactive Windows launcher | Windows |
| `start-server.sh` | Interactive Linux/macOS launcher | Linux/macOS |
| `ftps-server.service` | Systemd service template | Linux |

## 🚀 Quick Start

### Windows (PowerShell)
```powershell
# Generate certificate
New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"

# Build and run
dotnet restore
dotnet run
```

### Linux/macOS
```bash
# Generate certificate
./start-server.sh  # Choose option 4

# Build and run
dotnet restore
dotnet run
```

### Using Pre-made Scripts
```bash
# Windows
start-server.bat

# Linux/macOS
chmod +x start-server.sh
./start-server.sh
```

## ⚙️ Configuration Methods

### Method 1: JSON File (Recommended)
Edit `appsettings.json`:
```json
{
  "ServerSettings": {
    "IpAddress": "127.0.0.1",
    "Port": 21990
  },
  "Users": [
    {
      "Username": "admin",
      "Password": "password123",
      "RootFolder": "/",
      "Permissions": {
        "Read": true,
        "Write": true,
        "Delete": true,
        "CreateDirectory": true,
        "DeleteDirectory": true,
        "Rename": true
      }
    }
  ]
}
```

### Method 2: Command-Line Arguments
```bash
dotnet run -- --ip 0.0.0.0 --port 2121 \
  --user admin:pass123:/home/admin:RWDCXN \
  --user guest:guest:/public:R \
  --cert server.pfx --certpass mypassword
```

### Method 3: Mix Both
```bash
# JSON provides base config, CLI overrides specific settings
dotnet run -- --config production.json --port 3000
```

## 👥 Default Users

| Username | Password | Root Folder | Permissions |
|----------|----------|-------------|-------------|
| admin | password123 | / | Full access (RWDCXN) |
| readonly | readonly123 | /public | Read only (R) |
| user | user123 | /users/user | Full access (RWDCXN) |

⚠️ **Change these passwords before production use!**

## 🔐 Permission System

Each user can be assigned these permissions:

| Flag | Permission | Operations Allowed |
|------|------------|--------------------|
| R | Read | LIST, NLST, RETR, SIZE, MDTM |
| W | Write | STOR (upload files) |
| D | Delete | DELE (delete files) |
| C | Create Dirs | MKD (create directories) |
| X | Delete Dirs | RMD (remove directories) |
| N | Rename | RNFR/RNTO (rename files/folders) |

**Common Combinations:**
- `RWDCXN` - Full access (admin users)
- `R` - Read-only (public downloads)
- `RW` - Read and write, no delete
- `RWC` - Read, write, create directories
- `W` - Write-only (upload drop box)

## 📊 Logging with NLog

**Log Locations:**
- Console: Real-time colored output
- File: `logs/ftps-YYYY-MM-DD.log`
- Archives: `logs/archives/` (30-day retention)

**What's Logged:**
- ✅ Client connections/disconnections
- ✅ Authentication attempts (success/failure)
- ✅ File operations (upload/download/delete)
- ✅ Directory operations (create/delete/rename)
- ✅ Permission denials
- ✅ Errors and exceptions

**Sample Log Entry:**
```
2024-01-15 10:30:15.1234|INFO|Client connected: 192.168.1.100:54321 (Active: 1)
2024-01-15 10:30:16.2345|INFO|[192.168.1.100:54321] User logged in: admin
2024-01-15 10:30:18.3456|INFO|[192.168.1.100:54321] Uploading: /documents/report.pdf
2024-01-15 10:30:20.4567|INFO|[192.168.1.100:54321] Upload complete: /documents/report.pdf (2.5 MB)
```

## 🔧 Customization Examples

### Example 1: Corporate FTP Server
```json
{
  "ServerSettings": {
    "IpAddress": "0.0.0.0",
    "Port": 21,
    "MaxConnections": 100
  },
  "Users": [
    {
      "Username": "sales",
      "RootFolder": "/departments/sales",
      "Permissions": { "Read": true, "Write": true }
    },
    {
      "Username": "finance",
      "RootFolder": "/departments/finance",
      "Permissions": { "Read": true, "Write": true }
    }
  ]
}
```

### Example 2: Public Download Server
```bash
dotnet run -- \
  --ip 0.0.0.0 \
  --port 21 \
  --user guest:public:/downloads:R
```

### Example 3: File Upload Drop Box
```json
{
  "Users": [
    {
      "Username": "uploader",
      "RootFolder": "/uploads",
      "Permissions": {
        "Read": false,
        "Write": true,
        "CreateDirectory": true
      }
    }
  ]
}
```

## 🛡️ Security Features

1. **Path Traversal Protection** - Users cannot access files outside their root folder
2. **TLS Encryption** - All connections can be encrypted
3. **Per-User Permissions** - Granular control over operations
4. **Failed Login Logging** - Track authentication attempts
5. **Audit Trail** - Complete logging of all operations
6. **Connection Limits** - Prevent DOS attacks
7. **Certificate Support** - Custom SSL certificates

## 📈 Production Deployment

### Linux (systemd)
1. Publish release: `dotnet publish -c Release`
2. Copy files to: `/opt/ftps-server/`
3. Create user: `sudo useradd -r ftpuser`
4. Copy `ftps-server.service` to `/etc/systemd/system/`
5. Enable: `sudo systemctl enable ftps-server`
6. Start: `sudo systemctl start ftps-server`

### Windows Service
1. Use NSSM or sc.exe to create service
2. Configure startup type
3. Set recovery options

### Docker (create your own Dockerfile)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY publish/ .
EXPOSE 21990
ENTRYPOINT ["dotnet", "FtpsServer.dll"]
```

## 🔍 Monitoring & Troubleshooting

### Check Server Status
```bash
# Is it running?
netstat -an | grep 21990

# View active connections
grep "Client connected" logs/ftps-$(date +%Y-%m-%d).log

# Failed logins
grep "Failed login" logs/*.log

# File uploads today
grep "Upload complete" logs/ftps-$(date +%Y-%m-%d).log
```

### Common Issues

**Port in use:**
```bash
# Find what's using the port
netstat -ano | findstr :21990  # Windows
lsof -i :21990                 # Linux/macOS

# Use different port
dotnet run -- --port 2121
```

**Certificate errors:**
```bash
# Generate new certificate
openssl req -x509 -newkey rsa:4096 -keyout server.key -out server.crt -days 365 -nodes
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
```

**Permission denied:**
```bash
# Linux - for ports < 1024
sudo dotnet run
# or
sudo setcap CAP_NET_BIND_SERVICE=+eip /usr/bin/dotnet
```

## 🌐 Connecting to Server

### FileZilla
1. Protocol: FTP
2. Host: 127.0.0.1
3. Port: 21990
4. Encryption: Require explicit FTP over TLS
5. User: admin
6. Password: password123

### WinSCP
1. File protocol: FTP
2. Encryption: TLS/SSL Explicit encryption
3. Configure host, port, username, password

### Command Line (lftp)
```bash
lftp -u admin,password123 -e "set ftp:ssl-force true; set ssl:verify-certificate no" 127.0.0.1:21990
```

### cURL
```bash
# List files
curl -k --ftp-ssl -u admin:password123 ftp://127.0.0.1:21990/

# Download
curl -k --ftp-ssl -u admin:password123 ftp://127.0.0.1:21990/file.txt -o file.txt

# Upload
curl -k --ftp-ssl -u admin:password123 -T file.txt ftp://127.0.0.1:21990/
```

## 📚 Documentation Guide

1. **New to the project?** → Start with `QUICKSTART.md`
2. **Setting up for production?** → Read `README_Enhanced.md`
3. **Need reference?** → Check `README.md`
4. **Want to customize?** → Edit `appsettings.json` and review examples
5. **Troubleshooting?** → Check logs in `logs/` directory

## 🎯 Use Case Scenarios

### Scenario 1: Small Business File Sharing
- **Users**: Employees with personal folders
- **Config**: Per-user root folders with full permissions
- **Security**: TLS enabled, strong passwords, internal network only

### Scenario 2: Customer Upload Portal
- **Users**: Single upload account, admin account
- **Config**: Upload user has write-only, admin has full access
- **Security**: Public-facing, strong TLS, separate upload folder

### Scenario 3: Software Distribution
- **Users**: Single read-only account
- **Config**: Public read access to download folder
- **Security**: TLS optional, but recommended

### Scenario 4: Department File Server
- **Users**: Multiple departments with isolated folders
- **Config**: Each department has own folder and permissions
- **Security**: TLS enabled, audit logging, internal network

## 🔄 Migration from Old Server

If you have an existing FTP server:

1. **Copy files** to new root directory (`./ftproot`)
2. **Create users** in `appsettings.json` matching old accounts
3. **Test locally** before switching over
4. **Update client configurations** to new server IP/port
5. **Monitor logs** for any issues during transition

## 📞 Support Resources

- **Configuration Issues**: Review `appsettings.json` examples
- **Connection Problems**: Check firewall and certificate
- **Permission Errors**: Review user permissions in config
- **Performance Issues**: Check logs and increase MaxConnections
- **Security Concerns**: Review security section in README_Enhanced.md

## 🏆 Best Practices

1. ✅ Always use TLS in production
2. ✅ Change default passwords immediately
3. ✅ Use strong passwords (12+ characters)
4. ✅ Regularly review logs for suspicious activity
5. ✅ Keep .NET runtime updated
6. ✅ Backup configuration files
7. ✅ Set appropriate file system permissions
8. ✅ Use firewall rules to restrict access
9. ✅ Monitor disk space for uploads
10. ✅ Implement regular backups

## 🆚 Comparison with Alternatives

| Feature | This Server | FileZilla Server | vsftpd | ProFTPD |
|---------|-------------|------------------|---------|---------|
| Platform | Cross-platform | Windows | Linux | Linux |
| Language | C# | C++ | C | C |
| GUI | No (CLI) | Yes | No | No |
| TLS/SSL | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Logging | NLog | Built-in | Syslog | Syslog |
| Config | JSON/CLI | GUI/XML | Config file | Config file |
| Permissions | Per-user granular | Per-user | System | System |
| Customizable | ✅ Full source | Limited | Limited | Limited |

## 🔮 Future Enhancements (Ideas)

- [ ] Web-based admin panel
- [ ] SFTP support (SSH File Transfer)
- [ ] Database user storage
- [ ] Bandwidth throttling
- [ ] Quota management
- [ ] Virtual folders
- [ ] Active Directory integration
- [ ] Two-factor authentication
- [ ] API for user management
- [ ] Metrics and statistics dashboard

## ⚖️ License & Usage

This is a sample implementation provided for:
- ✅ Learning and education
- ✅ Internal corporate use
- ✅ Personal projects
- ✅ Modification and customization

Not recommended for:
- ⚠️ Internet-facing production without security review
- ⚠️ Mission-critical systems without testing
- ⚠️ Compliance-required environments without audit

## 🙏 Acknowledgments

Built using:
- **.NET 6.0+** - Microsoft's cross-platform framework
- **NLog** - Leading .NET logging library
- **System.Net** - Built-in networking libraries

## 📋 Checklist Before Going Live

- [ ] Certificate configured (not self-signed)
- [ ] Strong passwords set
- [ ] Firewall rules configured
- [ ] Logging enabled and monitored
- [ ] Backup strategy in place
- [ ] Tested with actual FTP clients
- [ ] User permissions reviewed
- [ ] Security audit completed
- [ ] Documentation updated for your team
- [ ] Monitoring configured

---

**Ready to get started?** Open `QUICKSTART.md` for a 5-minute setup guide!

**Need detailed help?** See `README_Enhanced.md` for comprehensive documentation!

**Questions?** Check the logs in `logs/` directory for detailed information about any issues.
