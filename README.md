## Enhanced C# FTPS .Net 10 Server & Library

A FTPS (FTP over TLS) server implementation in C# with advanced logging, user permissions, and flexible configuration options.
Project is splitted into Windows UI + Console App and Library. Aimed for Windows, Linux, or macOS platforms.

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

- ✅ **JSON File Configuration**
- ✅ **Command-Line Arguments** - Override any setting via CLI
- ✅ **User Permissions** - Granular control over Read/Write operations
- ✅ **Per-User Root Folders** - Isolated directories for each user
- ✅ **TLS/SSL Encryption** - Secure FTPS connections
- ✅ **Full FTP Protocol** - All standard FTP commands supported
- ✅ **Path Security** - Protection against directory traversal attacks

## Prerequisites

- .NET 10.0 SDK.

## Library

Can be found in nuget package manager: https://www.nuget.org/packages/Siarhei_Kuchuk.FtpsServerLibrary . Examples of code and configuration is described there.

## Windows Server Application

Tool can be obtained from Releases section. Supports ARM64 and X-64 platforms.

<img width="1133" height="636" alt="image" src="https://github.com/user-attachments/assets/b0bd5787-3d0a-4dc7-b7f9-b0dada250008" />

## ⚙️ Console Application Configuration

Tool can be obtained from Releases section.

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
| Users[].Folder                          | Yes       |               | User's folder.                                                                  |
| Users[].Read                            | Yes       |               | Can user read folder contents and download files.                               |
| Users[].Write                           | Yes       |               | Can user create, upload, write, delete, rename operations on files and folders. |

If certificate is not specified, self-signed certificate will be created and stored in %localappdata%\FtpsServerLibrary\Certificates.

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
  --user "admin#admin#F:\ftp server\admin#RW" \
  --user "reader#read123#F:\ftp server\reader#R" \
  --user "dropbox#dropbox123#F:\ftp server\dropbox#W

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
| --user "admin#admin#F:\\ftp server\\admin#RW"         |  |  | User with login admin and password admin with foilder F:\ftp server\admin with Read and Write permissions. |
| --user "reader#read123#F:\\ftp server\\reader#R"      |  |  | User with login admin and password read123 with foilder F:\ftp server\reader with Read permission.         |
| --user "dropbox#dropbox123#F:\\ftp server\\dropbox#W" |  |  | User with login admin and password dropbox123 with foilder F:\ftp server\dropbox with Write permission.    |

If certificate is not specified, self-signed certificate will be created and stored in %localappdata%\FtpsServerLibrary\Certificates.

### Method 3: Mix Both

```bash
# JSON provides base config, CLI overrides specific settings
dotnet run -- --config production.json --port 3000
```

## 🚀 Run

a. Download server sources and extract it to some folder.

b. Install Microsoft .Net 10 SDK.

c. Open terminal.

d. Navigate to extracted sources folder.

e. Restore NUGet packages

```bash
dotnet restore
```

c. Run 
 a 
## 🚀 Quick Start

```bash
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

## 🎯 Use Cases

- 1. Public Download Server (Read-Only)
- 2. File Upload Drop Box
- 3. Personal User Workspace

## Generate Self-Signed Certificate

Library will generate self-signed certificate, if no certificate source options are specified.
But you can also generate self-signed certificate on your own.

### Windows (PowerShell)

```powershell
# Generate certificate
New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
```

### Linux/macOS

You can do this via provided script

```bash
# Generate certificate
./start-server.sh  # Choose option 4
```

or via OpenSSL:

```bash
openssl req -x509 -newkey rsa:4096 -keyout server.key -out server.crt -days 365 -nodes -subj "/CN=localhost"
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt -password pass:yourpassword
```

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


### 4. Run the Server

```bash
# Using defaults (looks for appsettings.json)
dotnet run

# Using command-line arguments
dotnet run -- --ip 0.0.0.0 --port 2121 --user admin#pass123#/home/admin#RW

# Using custom config file
dotnet run -- --config myconfig.json

# With certificate
dotnet run -- --cert server.pfx --certpass yourpassword
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

