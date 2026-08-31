# Privacy policy

**FTPS Server** by Siarhei Kuchuk  
Last updated: 31 August 2026

The software is a local FTPS (FTP over TLS) server. It does not create cloud accounts. The developer does not operate a backend that receives your files, passwords, or usage data.

## Data the developer does not collect

The app does not include ads, analytics, crash reporters, or tracking SDKs. The developer does not collect, sell, or share personal data.

## Data stored on your device

Settings stay in app-private storage on the device (on Android, `settings.json` in the app’s files directory). That can include:

- Server port and connection limits
- FTPS usernames and passwords you create
- Paths or folder URIs you choose to share
- TLS certificate path and password, if you supply your own certificate

Those values are not uploaded to the developer. Uninstalling the app removes them. Shared **files** stay in the folders you picked; the app does not copy them to a developer server.

On Android, folders are selected with the system document picker. The app only uses folders you grant.

## Network use

### Update check

Installs from **F-Droid**, **Google Play**, or **RuStore** do not check for updates in the app; those stores handle updates.

Otherwise (GitHub APK, sideload) the app may request the latest GitHub release:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) receives a normal HTTPS request (IP address, user-agent, time). The developer does not receive that traffic.

### FTPS server

While the server is running, it listens on your local network so FTP clients you configure can read or write the folders you shared, using the usernames and passwords you set. That traffic stays between your devices (and anyone on the network who has those credentials). The developer is not a party to it.

You are responsible for who can reach the port, which folders you share, and how strong those passwords are.

### Links you open

Buttons that open the project page, license, or a GitHub release use the system browser. Those sites have their own privacy policies.

## Permissions (Android)

The app may use:

- **Internet** — update check and the FTPS server
- **Wi-Fi / network state** — show local addresses
- **Notifications and a foreground service** — keep transfers running with the screen off
- **Wake lock** — reduce sleeps during transfers

It does not request contacts, location, camera, microphone, or broad photo/media libraries.

## Children

The app is a network file server. It is not directed at children under 13.

## Third parties

App stores (Google Play, RuStore, Huawei AppGallery, F-Droid, and similar) process installs and reviews under their own policies. GitHub processes the optional update-check request as above.

## Changes

Updates to this policy will be posted in this file in the project repository.

## Contact

Questions: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
