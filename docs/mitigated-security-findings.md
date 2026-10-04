# Mitigated security findings

Review date: 4 October 2026.

These items were reviewed and accepted. A later review should skip them. Reopen an item only if the code that the decision relies on has changed.

The server listens on IPv4 only (`0.0.0.0`). File commands still require TLS and a password. Failed logins from one address are limited to 5 attempts, then a 1 second delay and a 60 second lockout.

## 1. Listener binds every interface

- **Status:** Accepted
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/Models.kt`, `FtpsServer.kt`

`FtpsServerSettings.ip` stays `0.0.0.0` so a phone or Android TV on the LAN works without an address setting. The listener is IPv4. An outsider connecting to the device’s IPv6 address does not reach this port. On a home network the router drops unsolicited IPv4; reaching the port takes an explicit forward or a public IPv4 address. A client that does connect still cannot run a file command without a valid account.

Binding to Wi-Fi only was considered and rejected. Another device on the same Wi-Fi could still connect, a router port-forward would still work, and the change would drop VPN peers and could miss USB Ethernet and the phone’s hotspot.

## 2. Listener binds every interface

- **Status:** Accepted
- **Platform:** .NET
- **Location:** `sources/FtpsServerLibrary/FtpsServerSettings.cs`, `FtpsServer.cs`; hosts set `0.0.0.0` in `sources/FtpsServerWindows/MainWindow.xaml.cs`, `sources/FtpsServerAvalonia/FtpsServerAvalonia/MainWindow.axaml.cs`, and `sources/FtpsServerAvalonia/FtpsServerAvalonia/Views/AndroidView.axaml.cs`

The library default remains `0.0.0.0`. That is the usual “listen on this machine” choice for the Windows app, the Avalonia desktop app, and the console host. The listener is IPv4, same as item 1. The console can still be given a single address with `--ip`. The Avalonia Android view uses the same address for the same reason as the Kotlin app.
