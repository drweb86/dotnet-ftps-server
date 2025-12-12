# FTPS Server Application

A modern WPF application for hosting an FTPS (FTP over SSL/TLS) server with both simple and advanced configuration modes.

## Features

### Simple Mode
- Quick setup with minimal configuration
- Single user account (admin/admin)
- Self-signed certificate (auto-generated)
- Listens on all network interfaces (0.0.0.0)
- Port 2121
- Easy folder selection
- Read/Write permission toggles

### Advanced Mode
- Multiple user accounts
- Custom server IP and port configuration
- Adjustable maximum connections
- Per-user folder access
- Per-user read/write permissions
- Certificate options:
  - Self-signed (auto-generated)
  - From file (.pfx, .pem, .der)
  - Password-protected certificates

### Additional Features
- Settings persistence between application restarts
- Modern dark-themed UI with cyan accents
- Real-time server status indicator
- Easy user management (add/remove users)

## Requirements

- .NET 8.0 SDK or later
- Windows OS
- Visual Studio 2022 (or Visual Studio Code with C# extension)

## Building the Application

1. Open a terminal in the project directory
2. Run the build command:
   ```bash
   dotnet build
   ```

3. To run the application:
   ```bash
   dotnet run
   ```

## Usage

### Simple Mode

1. Select "Simple Mode" tab
2. Click "Browse" to select a root folder
3. Choose permissions (Read/Write)
4. Click "START SERVER"
5. Connect using FTP client with:
   - Host: Your machine's IP address
   - Port: 2121
   - Username: admin
   - Password: admin
   - Connection type: Explicit FTPS (FTP over TLS)

### Advanced Mode

1. Select "Advanced Mode" tab
2. Configure server settings:
   - IP Address (0.0.0.0 for all interfaces)
   - Port (default: 2121)
   - Max Connections

3. Choose certificate source:
   - Self-Signed: Automatically generated
   - From File: Select .pfx, .pem, or .der file

4. Add users:
   - Click "+ Add User"
   - Enter username and password
   - Select root folder for the user
   - Set permissions (Read/Write)
   - Add multiple users as needed

5. Click "START SERVER"

## Connecting with FTP Clients

### FileZilla
1. File → Site Manager → New Site
2. Protocol: FTP - File Transfer Protocol
3. Encryption: Require explicit FTP over TLS
4. Host: Your server IP
5. Port: 2121 (or custom port)
6. Username/Password: As configured
7. Click Connect

### WinSCP
1. New Session
2. File protocol: FTP
3. Encryption: TLS/SSL Explicit encryption
4. Host name: Your server IP
5. Port: 2121 (or custom port)
6. Username/Password: As configured
7. Click Login

### Command Line (using curl)
```bash
curl -k --ftp-ssl --user username:password ftp://server-ip:2121/
```

## Security Notes

- **Self-signed certificates**: May show warnings in FTP clients. This is normal for self-signed certificates.
- **Production use**: For production environments, use a properly signed certificate from a Certificate Authority.
- **Firewall**: Ensure port 2121 (or your custom port) is open in your firewall.
- **Strong passwords**: Use strong passwords for user accounts in production.

## Settings Storage

Application settings are stored in:
```
%LocalAppData%\FtpsServerApp\settings.json
```

This includes:
- Mode selection (Simple/Advanced)
- Server configuration
- User accounts (passwords are stored in plain text - use appropriate security measures)
- Certificate settings

## Troubleshooting

### Server won't start
- Check if port 2121 is already in use
- Ensure folder paths exist and are accessible
- Verify certificate file path and password (if using file-based certificate)

### Can't connect from FTP client
- Verify firewall settings
- Check that server is running (green status indicator)
- Ensure using correct IP, port, and credentials
- Try using explicit FTPS mode in client

### Certificate errors
- For self-signed certificates, accept the certificate warning in your FTP client
- Ensure certificate file exists and password is correct
- Check certificate hasn't expired

## License

This application uses the FtpsServerLibrary NuGet package:
- Package: Siarhei_Kuchuk.FtpsServerLibrary
- Version: 2025.11.22

## Support

For issues with the FTPS server library, please refer to the package documentation.
