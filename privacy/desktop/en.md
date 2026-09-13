[Languages](README.md)

# Privacy policy

Last updated: 13 September 2026

**FTPS Server** by Siarhei Kuchuk

Application name: FTPS Server
Developer name: Siarhei Kuchuk

The software is a local FTPS (FTP over TLS) server. It does not create cloud accounts.
The developer does not operate a backend that receives your files, passwords, or usage data.

## Data the developer does not collect

The app does not include ads, analytics, crash reporters, or tracking SDKs. The developer does not collect, sell, or share personal data.

## Data stored on your computer

Application settings (including FTPS usernames and passwords, the server port, connection limits, and optional certificate path and password) are stored only on this computer:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

If the app creates a self-signed certificate, it is stored under:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Server logs may be written under:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Those values are not uploaded to the developer. Removing the app or those folders deletes them. Shared **files** stay in the folders you picked; the app does not copy them to a developer server.

Folders are selected with the system folder picker. The app shares only folders you grant.

No developer server is used to store your data.

## Network use

### Update check

The app may request the latest GitHub release:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) receives a normal HTTPS request (IP address, user-agent, time). The developer does not receive that traffic.

### FTPS server

While the server is running, it listens on your local network so FTPS clients you configure can read or write the folders you shared, using the usernames and passwords you set. That traffic stays between your devices (and anyone on the network who has those credentials). The developer is not a party to it.

You are responsible for who can reach the port, which folders you share, and how strong those passwords are.

### Links you open

The app can open these pages in your system browser. Those sites have their own privacy policies:

- Project homepage: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- License: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Latest release: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Other local behavior

While the server is running, the app may ask the operating system to reduce sleep so transfers can continue.

## Children

The app is a network file server. It is not directed at children under 13.

## Third parties

GitHub processes the update-check request and pages you open, as above. The developer does not receive that traffic.

## Changes

Updates to this policy will be posted in this file in the project repository.

## Contact

Application name: FTPS Server
Developer name: Siarhei Kuchuk

Questions: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
