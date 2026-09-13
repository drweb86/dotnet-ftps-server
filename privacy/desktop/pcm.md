[Languages](README.md)

# Privacy policy

Last update: 13 September 2026


**FTPS Server** by Siarhei Kuchuk

App name: FTPS Server
Developer name: Siarhei Kuchuk

Di software na local FTPS (FTP for TLS) server. E no dey create cloud account.
Di developer no dey run backend wey go receive your files, passwords, or usage data.

## Data wey di developer no dey collect

Di app no get ads, analytics, crash reporters, or tracking SDKs. Di developer no dey collect, sell, or share personal data.

## Data wey dey store for your computer

App settings (including FTPS usernames and passwords, server port, connection limits, and optional certificate path and password) dey store only for dis computer:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

If di app create self-signed certificate, e dey store here:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Server logs fit write here:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Dem no dey upload those values to di developer. If you remove di app or those folders, e go delete dem. Shared **files** go remain for di folders wey you pick; di app no dey copy dem to developer server.

Folders na system folder picker dey select. Di app only dey share folders wey you grant.

Dem no dey use developer server to store your data.

## Network use

### Update check

Di app fit request di latest GitHub release:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) dey receive normal HTTPS request (IP address, user-agent, time). Di developer no dey receive dat traffic.

### FTPS server

When di server dey run, e dey listen for your local network so FTPS clients wey you configure fit read or write di folders wey you share, with di usernames and passwords wey you set. Dat traffic remain between your devices (and anybody for di network wey get those credentials). Di developer no be party to am.

Na you dey responsible for who fit reach di port, which folders you share, and how strong those passwords be.

### Links wey you open

Di app fit open these pages for your system browser. Those sites get their own privacy policies:

- Project homepage: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- License: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Latest release: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Other local behaviour

When di server dey run, di app fit ask di operating system to reduce sleep so transfers fit continue.

## Children

Di app na network file server. E no dey for children under 13.

## Third parties

GitHub dey process di update-check request and pages wey you open, as above. Di developer no dey receive dat traffic.

## Changes

Updates to dis policy go dey post for dis file for di project repository.

## Contact

App name: FTPS Server
Developer name: Siarhei Kuchuk

Questions: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
