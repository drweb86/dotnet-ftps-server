# Quick Start Guide - FTPS Server

## 5-Minute Setup

### Step 1: Install Prerequisites
- Download and install .NET 10.0 SDK or Runtime
- Get it from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

### Step 2: Run the Application
```bash
# Option A: Double-click
run.bat

# Option B: Command line
dotnet run
```

### Step 3: Configure Simple Mode (Default)
1. **Select a folder to share**
   - Click "Browse" button
   - Navigate to folder (e.g., `C:\SharedFiles`)
   - Click "Select Folder"

2. **Set permissions**
   - ✓ Read - Allow downloads
   - ✓ Write - Allow uploads
   
3. **Click "START SERVER"**
   - Wait for success message
   - Status changes to "Server Running" (green)

### Step 4: Find Your IP Address
```bash
# Windows Command Prompt
ipconfig

# Look for "IPv4 Address"
# Example: 192.168.1.100
```

### Step 5: Connect from Client

**Using FileZilla:**
1. Download: https://filezilla-project.org/
2. Quick Connect:
   - Host: `ftps://192.168.1.100` (your IP)
   - Username: `admin`
   - Password: `admin`
   - Port: `2121`
3. Click "Quickconnect"
4. Accept certificate warning

**Using Windows Explorer:**
```
ftp://admin:admin@192.168.1.100:2121
```
Note: Windows Explorer may not support FTPS properly. Use FileZilla instead.

## Common Scenarios

### Scenario 1: Share Files with Family
```
Mode: Simple
Folder: C:\Family Photos
Permissions: Read + Write
Users can: Upload and download photos
```

### Scenario 2: Backup Collection Point
```
Mode: Simple
Folder: C:\Backups
Permissions: Write only
Users can: Upload backups only
```

### Scenario 3: Multiple Users (Work Team)
```
Mode: Advanced
Users:
  - alice / pass123 / C:\AliceFiles / Read+Write
  - bob / pass456 / C:\BobFiles / Read only
  - admin / adminpass / C:\AllFiles / Read+Write
```

## Troubleshooting - Quick Fixes

### Can't Start Server
```bash
# Problem: Port 2121 in use
# Solution: Change port in Advanced Mode to 2122

# Problem: Folder doesn't exist
# Solution: Create the folder first or choose existing folder
```

### Can't Connect
```bash
# Check 1: Is server running? (green indicator)
# Check 2: Correct IP address? (ipconfig)
# Check 3: Firewall blocking? (Add exception for port 2121)
# Check 4: On same network? (ping server-ip)
```

### Certificate Warning
```
# This is normal for self-signed certificates
# Solution: Click "Accept" or "Trust Always"
```

## Security Notes

⚠️ **Default Password**: Change `admin/admin` in production!

⚠️ **Network Exposure**: Keep server on local network unless needed on internet

⚠️ **Firewall**: May need to allow port 2121 in Windows Firewall

✓ **Self-Signed Cert**: Shows warnings but provides encryption

## Windows Firewall Setup

```powershell
# Run PowerShell as Administrator
New-NetFirewallRule -DisplayName "FTPS Server" -Direction Inbound -LocalPort 2121 -Protocol TCP -Action Allow
```

Or manually:
1. Windows Security → Firewall & network protection
2. Advanced settings → Inbound Rules → New Rule
3. Port → TCP → Specific local ports: 2121
4. Allow the connection → Name: "FTPS Server"

## Next Steps

- Try Advanced Mode for multiple users
- Use custom certificate for production

## Support

- Check logs when errors occur
- Settings saved to: `%LocalAppData%\FtpsServerApp\settings.json`

---

**Happy File Sharing! 🚀**
