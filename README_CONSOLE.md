# FTPS Server Console Application

<img width="760" height="433" alt="image" src="https://github.com/user-attachments/assets/37f8d159-9188-4838-83d4-4ae1b64b7b65" />

**Logs** are located at ```%AppData%/ftps-server/logs```

**Self-Signed Certificate Location**

If certificate is not specified, self-signed certificate will be created and stored in ```%localappdata%\FtpsServerLibrary\Certificates```.

## Configuration

## Settings file

You can place `appsettings.json` file near executable (example can be taken below). When console detects file with this name near executable, it will load it.

You can place settings file in any location. To make console fetch it, inform it about its location by `--config some-configuiration.json`.

Configuration file is optional.

You can skip specifying certain parameters. In this case console will take it from command line arguments or manual input.

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

## Command Line Arguments

Powershell example with all arguments:

```bash
ftps-server.exe -- \
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

Powershell example with minimum arguments (self-signed certificate will be used, 10 parallel connections maximum):

```bash
ftps-server.exe -- \
  --ip 0.0.0.0  \
  --port 2121 \
  --user "admin#admin#F:\ftp server\admin#RW"

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

You can specify some settings in settings file and some settings as arguments. 
Specified by arguments parameters will have priority.

Example:

```bash
# JSON provides base config, CLI overrides specific settings
ftps-server.exe --config production.json --port 3000
```

## Input arguments interactively.

If you launch console application without arguments or without users, it will print help and ask you to input parameters manually.

When you input parameters, tool will propose to save it to file for future use with --config parameter.

## -----------------------------Information below is not verified yet.------------------------------

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


## 🎯 Use Cases

- 1. Public Download Server (Read-Only)
- 2. File Upload Drop Box
- 3. Personal User Workspace





## 🔍 Monitoring & Troubleshooting

### 4. Run the Server

```bash
# Using defaults (looks for appsettings.json)
ftps-server.exe

# Using command-line arguments
ftps-server.exe --ip 0.0.0.0 --port 2121 --user admin#pass123#/home/admin#RW

# Using custom config file
ftps-server.exe --config myconfig.json

# With certificate
ftps-server.exe --cert server.pfx --certpass yourpassword
```

## Troubleshooting

### Connection Issues

Linux has requirements for port to be above certain number.

To connect to VirtualBox, you have to do special configuration.




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

