# Mitigated security findings

Review date: 4 October 2026.

These items were reviewed and then fixed or accepted. A later review should skip them. Reopen an item only if the code that the decision relies on has changed.

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

## 9 and 10. Connection instructions include the password

- **Status:** Accepted
- **Platform:** Android Kotlin and .NET

The connection card, Copy, and Share include each account password, together with the host, port, encryption mode, login, and certificate fingerprint. That text is how the user sets up a client on another device after installing the app. The account form and the certificate-password field are masked until the user chooses Show. Do not remove the password from the card, from Copy, or from Share.

## Screenshots build

- **Status:** Accepted
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/build.gradle.kts` (`screenshots`), `BuildConfig.SCREENSHOTS`, `MainActivity.kt`

The normal app sets `FLAG_SECURE`, so the recent-apps thumbnail cannot capture the screen. A build made with `-Pscreenshots=true` does not set that flag, so store screenshots can still be taken. That build uses the package id `com.siarheikuchuk.ftpsserver.screenshots` and is not a release a user installs. Do not treat the missing flag on that build as a finding, and do not remove the screenshots build.

## TLS 1.3 data connections accept any resumed session on Windows and Android

- **Status:** Accepted (residual after hardening)
- **Platform:** .NET on Windows, Android Kotlin, Avalonia Android
- **Location:** `sources/FtpsServerLibrary/FtpsTlsSession.cs` (`SameAsControl`), `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/FtpsClientSession.kt` (`sameTlsSession`)

The data connection must resume the control connection's TLS session. Where a stable per-session value exists, it is compared: TLS 1.2 on every platform, and TLS 1.3 on OpenSSL (Linux), which restores the original session from the ticket. On Windows, Schannel reports a fresh session id for every resumed TLS 1.3 connection and only the reconnect flag survives (verified on Windows 11, .NET 10), so the resumed flag is the strongest signal available; Conscrypt on Android behaves the same, and the data socket also has `enableSessionCreation = false`, so only a session this server issued can pass. Avalonia on Android cannot read a session id at all and falls back to the peer-address check.

The residual risk: a client on the same source address as a victim (shared NAT) that resumes its own TLS 1.3 session and wins the race to the victim's passive port is accepted as the data channel. The attacker must share the victim's address, guess the ephemeral port, and beat the real client, so this is accepted. TLS 1.3 exporter keying material was considered and rejected: a resumed connection re-keys, so its exporter output differs from the control connection's.
