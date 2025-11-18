# Enhanced C# FTPS Server with NLog

A complete FTPS (FTP over TLS) server implementation in C# with advanced logging, user permissions, and flexible configuration options.

## Features

### Core Features
- **Authentication**: Username and password login with per-user permissions
- **TLS/SSL Encryption**: Secure control and data connections (AUTH TLS)
- **Advanced Logging**: Using NLog with file and console output
- **User Permissions**: Granular control (Read, Write, Delete, CreateDir, DeleteDir, Rename)
- **Per-User Root Folders**: Isolated directories for each user
- **Flexible Configuration**: Command-line arguments and JSON config file support

### Security Features
- Path traversal protection
- Per-user root directory isolation
- Granular permission system
- TLS/SSL encryption support
- Failed login attempt logging
- Comprehensive audit logging

## Installation & Setup

### Prerequisites

- .NET 10.0 SDK
- Windows, Linux, or macOS

### 1. Clone or Download Files

### 2. Build the Project

```bash
dotnet restore
dotnet build -c Release
```

Or build directly:
```bash
dotnet build
```

### 3. Generate SSL Certificate

**Windows (PowerShell - Run as Administrator):**
```powershell
New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(10)
```

**Linux/macOS (OpenSSL):**
```bash
openssl req -x509 -newkey rsa:4096 -keyout server.key -out server.crt -days 365 -nodes -subj "/CN=localhost"
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt -password pass:yourpassword
```

### 4. Run the Server

```bash
# Using defaults (looks for appsettings.json)
dotnet run

# Using command-line arguments
dotnet run -- --ip 0.0.0.0 --port 2121 --user admin:pass123:/home/admin:RWDCNX

# Using custom config file
dotnet run -- --config myconfig.json

# With certificate
dotnet run -- --cert server.pfx --certpass yourpassword
```

## Configuration

### Option 1: JSON Configuration File (Recommended)

Create or edit `appsettings.json`:

```json
{
  "ServerSettings": {
    "IpAddress": "127.0.0.1",
    "Port": 21990,
    "CertificatePath": "server.pfx",
    "CertificatePassword": "yourpassword",
    "MaxConnections": 10
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
    },
    {
      "Username": "readonly",
      "Password": "readonly123",
      "RootFolder": "/public",
      "Permissions": {
        "Read": true,
        "Write": false,
        "Delete": false,
        "CreateDirectory": false,
        "DeleteDirectory": false,
        "Rename": false
      }
    },
    {
      "Username": "user",
      "Password": "user123",
      "RootFolder": "/users/user",
      "Permissions": {
        "Read": true,
        "Write": true,
        "Delete": true,
        "CreateDirectory": true,
        "DeleteDirectory": true,
        "Rename": true
      }
    }
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Option 2: Command-Line Arguments

```bash
# General syntax
dotnet run -- [options]

# Available options:
--config <path>              # Path to JSON configuration file
--ip <address>               # IP address to bind (default: 127.0.0.1)
--port <number>              # Port number (default: 21990)
--cert <path>                # Certificate file path (.pfx)
--certpass <password>        # Certificate password
--user <name:pass:folder:permissions>  # Add user
--help                       # Show help message
```

### User Permission Format

When adding users via command line, use this format:
```
--user username:password:rootfolder:permissions
```

**Permission flags:**
- `R` = Read
- `W` = Write
- `D` = Delete files
- `C` = Create directories
- `X` = Delete directories  
- `N` = Rename files/directories

**Examples:**
```bash
# Full permissions
--user admin:pass123:/home/admin:RWDCXN

# Read-only user
--user guest:guest:/public:R

# Read and write, but no delete
--user user:pass:/users/user:RWC
```

### Configuration Examples

**Example 1: Basic Setup with JSON**
```bash
dotnet run -- --config appsettings.json
```

**Example 2: All Command-Line Options**
```bash
dotnet run -- \
  --ip 0.0.0.0 \
  --port 2121 \
  --cert /path/to/cert.pfx \
  --certpass mypassword \
  --user admin:adminpass:/home/admin:RWDCXN \
  --user user1:user1pass:/users/user1:RWCN \
  --user readonly:readpass:/public:R
```

**Example 3: Mix JSON and Command-Line (CLI overrides JSON)**
```bash
dotnet run -- --config appsettings.json --port 3000 --user newuser:newpass:/temp:RW
```

**Example 4: Minimal Setup**
```bash
dotnet run -- --user admin:admin:/home:RWDCXN
```

## Logging

The server uses **NLog** for comprehensive logging.

### Log Locations

- **Console**: Real-time colored output
- **File**: `logs/ftps-YYYY-MM-DD.log`
- **Archives**: `logs/archives/` (automatic daily rotation, 30-day retention)

### Log Levels

Configured in `NLog.config`:
- **Debug**: All FTP commands and responses
- **Info**: Connection events, authentication, file operations
- **Warn**: Failed login attempts, permission denials
- **Error**: Command errors, transfer failures
- **Fatal**: Server startup/shutdown errors

### Sample Log Output

```
2024-01-15 10:30:15.1234|INFO|Client connected: 192.168.1.100:54321 (Active: 1)
2024-01-15 10:30:16.2345|INFO|[192.168.1.100:54321] User logged in: admin
2024-01-15 10:30:18.3456|INFO|[192.168.1.100:54321] Uploading: /documents/report.pdf
2024-01-15 10:30:20.4567|INFO|[192.168.1.100:54321] Upload complete: /documents/report.pdf (2.5 MB)
```

### Customizing Logging

Edit `NLog.config` to customize:
```xml
<rules>
  <!-- Change minlevel to control verbosity -->
  <logger name="*" minlevel="Info" writeTo="logfile" />
  <logger name="*" minlevel="Info" writeTo="coloredConsole" />
</rules>
```

## User Permissions Explained

Each user can have the following permissions:

| Permission | Description | FTP Commands Affected |
|------------|-------------|----------------------|
| **Read** | List and download files | LIST, NLST, RETR, SIZE, MDTM |
| **Write** | Upload files | STOR |
| **Delete** | Delete files | DELE |
| **CreateDirectory** | Create folders | MKD, XMKD |
| **DeleteDirectory** | Delete folders | RMD, XRMD |
| **Rename** | Rename files/folders | RNFR, RNTO |

### Permission Scenarios

**Full Access Admin:**
```json
{
  "Username": "admin",
  "Password": "admin123",
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
```

**Read-Only User:**
```json
{
  "Username": "readonly",
  "Password": "view123",
  "RootFolder": "/public",
  "Permissions": {
    "Read": true,
    "Write": false,
    "Delete": false,
    "CreateDirectory": false,
    "DeleteDirectory": false,
    "Rename": false
  }
}
```

**Upload-Only User (for submissions):**
```json
{
  "Username": "uploader",
  "Password": "upload123",
  "RootFolder": "/uploads",
  "Permissions": {
    "Read": false,
    "Write": true,
    "Delete": false,
    "CreateDirectory": true,
    "DeleteDirectory": false,
    "Rename": false
  }
}
```

## Connecting to the Server

### Using FileZilla

1. Open FileZilla
2. Go to **File → Site Manager**
3. Click **New Site**
4. Configure:
   - **Protocol**: FTP - File Transfer Protocol
   - **Host**: 127.0.0.1 (or your server IP)
   - **Port**: 21990 (or your configured port)
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
5. Port number: **21990**
6. User name: **admin**
7. Password: **password123**
8. Click **Login**

### Using lftp (Linux/macOS)

```bash
lftp -u admin,password123 -e "set ftp:ssl-force true; set ftp:ssl-protect-data true; set ssl:verify-certificate no" 127.0.0.1:21990
```

### Using Command Line (curl)

```bash
# List files
curl -k --ftp-ssl -u admin:password123 ftp://127.0.0.1:21990/ --list-only

# Download file
curl -k --ftp-ssl -u admin:password123 ftp://127.0.0.1:21990/file.txt -o file.txt

# Upload file
curl -k --ftp-ssl -u admin:password123 -T file.txt ftp://127.0.0.1:21990/
```

## Monitoring & Management

### Check Active Connections

The server logs active connections in real-time:
```
Client connected: 192.168.1.100:54321 (Active: 1)
Client disconnected: 192.168.1.100:54321 (Active: 0)
```

### View Logs

```bash
# View today's log
tail -f logs/ftps-$(date +%Y-%m-%d).log

# Search for failed logins
grep "Failed login" logs/*.log

# View user activity
grep "admin" logs/ftps-$(date +%Y-%m-%d).log
```

### Security Monitoring

Important events to monitor:
- Failed login attempts
- Permission denied errors
- Unusual file operations
- Connection from unexpected IPs

## Troubleshooting

### Server Won't Start

**Port already in use:**
```
Change port: --port 2121 or edit appsettings.json
```

**Certificate errors:**
```bash
# Verify certificate exists
ls -la server.pfx

# Check certificate validity
openssl pkcs12 -info -in server.pfx -nodes
```

**Permission errors on Linux:**
```bash
# For ports < 1024, use sudo or capabilities
sudo dotnet run
# or
sudo setcap CAP_NET_BIND_SERVICE=+eip /usr/share/dotnet/dotnet
```

### Connection Issues

**Client can't connect:**
1. Check firewall rules
2. Verify server is running: `netstat -an | grep 21990`
3. Test locally first: `telnet 127.0.0.1 21990`
4. Check logs for errors

**TLS/SSL errors:**
1. Ensure certificate is valid
2. Client must support explicit TLS (AUTH TLS)
3. Try with certificate validation disabled (testing only)

**Passive mode failures:**
1. Check if client can reach server IP
2. Ensure data port range is accessible
3. Configure firewall for passive mode

### Permission Denied Errors

**User can't access files:**
1. Check user permissions in config
2. Verify RootFolder exists
3. Check filesystem permissions
4. Review logs for specific operation denied

**Can't go to directory:**
- Users are restricted to their RootFolder
- Check if path is within user's root

## Production Deployment

### Security Checklist

- [ ] Use proper CA-signed certificate (not self-signed)
- [ ] Use strong passwords (12+ characters, mixed case, numbers, symbols)
- [ ] Restrict IP addresses via firewall
- [ ] Use non-standard port (not 21)
- [ ] Run as non-privileged user
- [ ] Enable log rotation
- [ ] Monitor failed login attempts
- [ ] Regular security updates
- [ ] Backup configuration and data

### Recommended Settings

```json
{
  "ServerSettings": {
    "IpAddress": "0.0.0.0",
    "Port": 2121,
    "MaxConnections": 50,
    "CertificatePath": "/etc/ftps/certificate.pfx"
  }
}
```

### Running as a Service

**Linux (systemd):**

Create `/etc/systemd/system/ftps.service`:
```ini
[Unit]
Description=FTPS Server
After=network.target

[Service]
Type=simple
User=ftpuser
WorkingDirectory=/opt/ftps
ExecStart=/usr/bin/dotnet /opt/ftps/FtpsServer.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable ftps
sudo systemctl start ftps
sudo systemctl status ftps
```

**Windows (NSSM or sc.exe):**

Using NSSM (Non-Sucking Service Manager):
```cmd
nssm install FtpsServer "C:\Program Files\dotnet\dotnet.exe" "C:\ftps\FtpsServer.dll"
nssm start FtpsServer
```

### Performance Tuning

- Increase MaxConnections for high-traffic servers
- Use SSD storage for better I/O performance
- Monitor memory usage and adjust as needed
- Consider dedicated network interface
- Use log rotation to manage disk space

## Advanced Usage

### Multiple Root Directories Per User

Each user can have their own isolated directory:

```json
{
  "Users": [
    {
      "Username": "user1",
      "RootFolder": "/users/user1"
    },
    {
      "Username": "user2",
      "RootFolder": "/users/user2"
    }
  ]
}
```

### Shared and Private Directories

```json
{
  "Users": [
    {
      "Username": "admin",
      "RootFolder": "/"
    },
    {
      "Username": "sales",
      "RootFolder": "/departments/sales"
    },
    {
      "Username": "public",
      "RootFolder": "/public",
      "Permissions": { "Read": true, "Write": false }
    }
  ]
}
```

### Audit Logging

All operations are logged. Parse logs for auditing:

```bash
# Files uploaded today
grep "Upload complete" logs/ftps-$(date +%Y-%m-%d).log

# Files deleted
grep "Deleted file" logs/*.log

# User activity
grep "User logged in: admin" logs/*.log

# Failed operations
grep "Permission denied\|failed\|error" logs/*.log
```

## Supported FTP Commands

| Command | Description | Requires Auth |
|---------|-------------|---------------|
| USER | Username | No |
| PASS | Password | No |
| AUTH | Enable TLS | No |
| PBSZ | Protection buffer | Yes |
| PROT | Protection level | Yes |
| PWD/XPWD | Print directory | Yes |
| CWD/XCWD | Change directory | Yes |
| CDUP/XCUP | Parent directory | Yes |
| MKD/XMKD | Create directory | Yes |
| RMD/XRMD | Remove directory | Yes |
| DELE | Delete file | Yes |
| RNFR | Rename from | Yes |
| RNTO | Rename to | Yes |
| TYPE | Transfer type | Yes |
| PASV | Passive mode | Yes |
| LIST | List files (detailed) | Yes |
| NLST | List files (names) | Yes |
| RETR | Download file | Yes |
| STOR | Upload file | Yes |
| SIZE | Get file size | Yes |
| MDTM | Modification time | Yes |
| SYST | System info | Yes |
| FEAT | Feature list | Yes |
| OPTS | Options | Yes |
| NOOP | No operation | Yes |
| QUIT | Disconnect | No |

## Dependencies

This project uses **NLog** for logging:
- **NLog** (v5.2.8): Advanced .NET logging library
- **NLog.Extensions.Logging** (v5.3.8): Integration extensions

These are the only external dependencies. All other functionality uses built-in .NET libraries.

## License

This is a sample implementation for educational and internal use purposes.

## Support & Contributing

For issues, questions, or contributions:
1. Check the logs first
2. Review this documentation
3. Test with FileZilla or another standard client
4. Ensure configuration is correct

## Alternatives

For production environments, consider:
- **FileZilla Server**: Full-featured, GUI-based
- **ProFTPD**: Linux FTP server with TLS
- **vsftpd**: Very secure FTP daemon
- **SFTP (SSH)**: More secure than FTPS for many use cases

## Version History

- **2.0**: Added NLog logging, command-line arguments, JSON config, user permissions
- **1.0**: Basic FTPS server with authentication

---

**Note**: This server is designed for internal use and development purposes. For production internet-facing deployments, perform a thorough security review and consider using established FTP server software with regular security updates.
