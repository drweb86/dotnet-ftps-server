## Enhanced C# FTPS .Net 10 Server & Library

A FTPS (FTP over TLS) server implementation in C# with advanced logging, user permissions, and flexible configuration options.
Project is splitted into Console and Library. Aimed for all platforms.

This is a sample implementation provided for:
- ✅ Learning and education
- ✅ Internal corporate use
- ✅ Personal projects
- ✅ Modification and customization

Not recommended for:
- ⚠️ Internet-facing production without security review
- ⚠️ Mission-critical systems without testing
- ⚠️ Compliance-required environments without audit

## Features

✅ **JSON File Configuration**
✅ **Command-Line Arguments** - Override any setting via CLI
✅ **User Permissions** - Granular control over Read/Write operations
✅ **Per-User Root Folders** - Isolated directories for each user
✅ **TLS/SSL Encryption** - Secure FTPS connections
✅ **Full FTP Protocol** - All standard FTP commands supported
✅ **Path Security** - Protection against directory traversal attacks

### Prerequisites

- .NET 10.0 SDK
- Windows, Linux, or macOS

## ⚙️ Configuration Methods

### Method 1: JSON File

You can place `appsettings.json` file near executable (example can be taken from text below or `appsettings-example.json`).
Aternatively you can specify `--config some-configuiration.json`.
Having configuration file is optional.

`appsettings.json`:
```json
{
  "ServerSettings": {
    "Ip": "0.0.0.0",
    "Port": 2121,
    "MaxConnections": 10,
    "CertificatePath": "certificate.pfx",
    "CertificatePassword": "password",
    "CertificateStoreName": "My",
    "CertificateStoreLocation": "CurrentUser",
    "CertificateStoreSubject": "ftps server store subject"
  },
  "Users": [
    {
      "Login": "admin",
      "Password": "admin",
      "Folder": "F:\\ftp server\\admin",
      "Read": true,
      "Write": true
    },
    {
      "Login": "reader",
      "Password": "read123",
      "Folder": "F:\\ftp server\\reader",
      "Read": true,
      "Write": false
    },
    {
      "Login": "dropbox",
      "Password": "dropbox123",
      "Folder": "F:\\ftp server\\dropbox",
      "Read": false,
      "Write": true
    }
  ]
}
```

| Parameter                               | Required  | Default value | Remarks                                                                                                                 |
|-----------------------------------------|-----------|---------------|-------------------------------------------------------------------------------------------------------------------------|
| ServerSettings.Ip                       | No        | 0.0.0.0       | The IP address server will be listening to. 0.0.0.0 - listen on every available network interface.                      |
| ServerSettings.Port                     | No        | 2121          | The Port for server to listen to.                                                                                       |
| ServerSettings.MaxConnections           | No        | 10            | Maximum number of simultaneous server connections.                                                                      |
| ServerSettings.CertificatePath          | No        |               | PEM, DER or PKCS#12 PFX file. PFX file is opened with CertificatePassword (if specified).                               |
| ServerSettings.CertificatePassword      | No        |               | Certificate password. When specified, will be used for opening certificate from CertificatePath.                        |
| ServerSettings.CertificateStoreName     | No        |               | Certificate store name. Possible values: AuthRoot, CertificateAuthority, My, Root, TrustedPublisher. Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified. |
| ServerSettings.CertificateStoreLocation | No        |               | Certificate store location. Possible values: CurrentUser, LocalMachine. Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.                              |
| ServerSettings.CertificateStoreSubject  | No        |               | Certificate store subject by which certificate will be searched in certificate store and location. Used when CertificateStoreName, CertificateStoreLocation and CertificateStoreSubject are together specified.   |
| Users[].Login                           | Yes       |               | User's login.                                                                   |
| Users[].Password                        | Yes       |               | User's password.                                                                |
| Users[].Folder                          | Yes       |               | User's login.                                                                   |
| Users[].Read                            | Yes       |               | Can user read folder contents and download files.                               |
| Users[].Write                           | Yes       |               | Can user create, upload, write, delete, rename operations on files and folders. |

If certificate is not specified, self-signed certificate will be created and stored in %appdata%\FtpsServerLibrary\Certificates.

### Method 2: Command-Line Arguments

```bash
dotnet run -- \
  --help \
  --config <path to configuration json> \
  --ip 0.0.0.0  \
  --port 2121 \
  --maxconnections 10 \
  --cert server.pfx \
  --certpass mypassword \
  --certstorename My \
  --certstorelocation CurrentUser \
  --certstoresubject "ftps server store subject" \
  --user admin|admin|F:\\ftp server\\admin|RW \
  --user reader|read123|F:\\ftp server\\reader|R \
  --user dropbox|dropbox123|F:\\ftp server\\dropbox|W

```

| Command line argument                          | Required  | Default value                                     | Remarks                                                                                              |
|------------------------------------------------|-----------|---------------------------------------------------|------------------------------------------------------------------------------------------------------|
| --help                                         | No        |                                                   | Show the help message.                                                                               |
| --config configuration.json                    | No        | appsettings.json near executable file (if exists) | Path to JSON configuration file.                                                                     |
| --ip 0.0.0.0                                   | No        | 0.0.0.0                                           | The IP address server will be listening to. 0.0.0.0 - listen on every available network interface.   |
| --port 2121                                    | No        | 2121                                              | The Port for server to listen to.                                                                    |
| --maxconnections 10                            | No        | 10                                                | Maximum number of simultaneous server connections.                                                   |
| --cert file.pfx                                | No        |                                                   | PEM, DER or PKCS#12 PFX file. PFX file is opened with certpass (if specified).                       |
| --certpass password                            | No        |                                                   | Certificate password. When specified, will be used for opening certificate from cert.                |
| --certstorename My                             | No        |                                                   | Certificate store name. Possible values: AuthRoot, CertificateAuthority, My, Root, TrustedPublisher. Used when certstorename, certstorelocation and certstoresubject are together specified. |
| --certstorelocation CurrentUser                | No        |                                                   | Certificate store location. Possible values: CurrentUser, LocalMachine. Used when certstorename, certstorelocation and certstoresubject are together specified.                              |
| --certstoresubject "ftps server store subject" | No        |                                                   | Certificate store subject by which certificate will be searched in certificate store and location. Used when certstorename, certstorelocation and certstoresubject are together specified.   |
| --user admin\|admin\|F:\\ftp server\\admin\|RW         |  |  | User with login admin and password admin with foilder F:\ftp server\admin with Read and Write permissions. |
| --user reader\|read123\|F:\\ftp server\\reader\|R      |  |  | User with login admin and password read123 with foilder F:\ftp server\reader with Read permission.         |
| --user dropbox\|dropbox123\|F:\\ftp server\\dropbox\|W |  |  | User with login admin and password dropbox123 with foilder F:\ftp server\dropbox with Write permission.    |

If certificate is not specified, self-signed certificate will be created and stored in %appdata%\FtpsServerLibrary\Certificates.

### Method 3: Mix Both

```bash
# JSON provides base config, CLI overrides specific settings
dotnet run -- --config production.json --port 3000
```

## 📊 Monitoring

**Log Locations:**
- Location: `%AppData%/ftps-server/logs/ftps-YYYY-MM-DD.log`
- Real-time: Console shows colored output
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

**Check connections:**
- Console shows: `Client connected: IP:PORT (Active: N)`






















## 📁 File Listing

### Scripts

| File                  | Description                      | Platform    |
|-----------------------|----------------------------------|-------------|
| `start-server.bat`    | Interactive Windows launcher     | Windows     |
| `start-server.sh`     | Interactive Linux/macOS launcher | Linux/macOS |
| `ftps-server.service` | Systemd service template         | Linux       |










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
EXPOSE 2121
ENTRYPOINT ["dotnet", "FtpsServer.dll"]
```

## 🔍 Monitoring & Troubleshooting

### Check Server Status
```bash
# Is it running?
netstat -an | grep 2121

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
netstat -ano | findstr :2121  # Windows
lsof -i :2121                 # Linux/macOS

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
2. Host: 0.0.0.0
3. Port: 2121
4. Encryption: Require explicit FTP over TLS
5. User: admin
6. Password: password123

### WinSCP
1. File protocol: FTP
2. Encryption: TLS/SSL Explicit encryption
3. Configure host, port, username, password

### Command Line (lftp)
```bash
lftp -u admin,password123 -e "set ftp:ssl-force true; set ssl:verify-certificate no" 0.0.0.0:2121
```

### cURL
```bash
# List files
curl -k --ftp-ssl -u admin:password123 ftp://0.0.0.0:2121/

# Download
curl -k --ftp-ssl -u admin:password123 ftp://0.0.0.0:2121/file.txt -o file.txt

# Upload
curl -k --ftp-ssl -u admin:password123 -T file.txt ftp://0.0.0.0:2121/
```



## Installation & Setup

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
    "IpAddress": "0.0.0.0",
    "Port": 2121,
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
--ip <address>               # IP address to bind (default: 0.0.0.0)
--port <number>              # Port number (default: 2121)
--cert <path>                # Certificate file path (.pfx)
--certpass <password>        # Certificate password
--user <name:pass:folder:permissions>  # Add user
--help                       # Show help message
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
   - **Host**: 0.0.0.0 (or your server IP)
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
4. Host name: **0.0.0.0**
5. Port number: **2121**
6. User name: **admin**
7. Password: **password123**
8. Click **Login**

### Using lftp (Linux/macOS)

```bash
lftp -u admin,password123 -e "set ftp:ssl-force true; set ftp:ssl-protect-data true; set ssl:verify-certificate no" 0.0.0.0:2121
```

### Using Command Line (curl)

```bash
# List files
curl -k --ftp-ssl -u admin:password123 ftp://0.0.0.0:2121/ --list-only

# Download file
curl -k --ftp-ssl -u admin:password123 ftp://0.0.0.0:2121/file.txt -o file.txt

# Upload file
curl -k --ftp-ssl -u admin:password123 -T file.txt ftp://0.0.0.0:2121/
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
2. Verify server is running: `netstat -an | grep 2121`
3. Test locally first: `telnet 0.0.0.0 2121`
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
   - Host: `0.0.0.0`
   - Port: `2121`
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
   lftp -u admin,password123 -e "set ftp:ssl-force true; set ssl:verify-certificate no" 0.0.0.0:2121
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
- IP: `0.0.0.0` (localhost)
- Port: `2121`
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
- Verify server is running: `netstat -an | grep 2121`
- Try telnet test: `telnet 0.0.0.0 2121`

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
