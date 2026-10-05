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

The normal app sets `FLAG_SECURE` while the server is running and the connection-details section is expanded, because that section shows the account password in clear text. Collapsing the section, stopping the server, or leaving the main screen clears the flag, so a screenshot or recent-apps thumbnail can be taken when the password is not on screen. A build made with `-Pscreenshots=true` never sets the flag, so store screenshots can still be taken with the section open. That build uses the package id `com.siarheikuchuk.ftpsserver.screenshots` and is not a release a user installs. Do not treat the missing flag on that build as a finding, and do not remove the screenshots build.

## TLS 1.3 data connections accept any resumed session on Windows and Android

- **Status:** Accepted (residual after hardening)
- **Platform:** .NET on Windows, Android Kotlin, Avalonia Android
- **Location:** `sources/FtpsServerLibrary/FtpsTlsSession.cs` (`SameAsControl`), `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/FtpsClientSession.kt` (`sameTlsSession`)

The data connection must resume the control connection's TLS session where the platform can keep doing that for the whole control connection. TLS 1.2 on every platform, and TLS 1.3 on OpenSSL (Linux), compare a stable session id. On Windows, Schannel reports a fresh session id for every resumed TLS 1.3 connection, so the reconnect flag never identified the control session (verified on Windows 11, .NET 10). Schannel also stops resuming one ticket after a handful of data connections and completes a full handshake, which rejected every later listing. Windows TLS 1.3 therefore falls back to the peer-address check. Conscrypt on Android still requires a resumed session, and the data socket has `enableSessionCreation = false`, so only a session this server issued can pass. Avalonia on Android cannot read a session id at all and falls back to the peer-address check.

The residual risk: a client on the same source address as a victim (shared NAT) that wins the race to the victim's passive port is accepted as the data channel. On Linux and Android that client still has to resume a session this server issued. On Windows TLS 1.3 a full handshake from that address is enough, because requiring resumption there breaks a normal client and still would not prove the session was the control connection. The attacker must share the victim's address, guess the ephemeral port, and beat the real client. TLS 1.3 exporter keying material was considered and rejected: a resumed connection re-keys, so its exporter output differs from the control connection's. Sending a fresh ticket on the control connection before each data connection would keep resumption working, but Schannel through SslStream does not provide a way to emit one.

## 9 (lifetime). Self-signed certificate is valid for 10 years

- **Status:** Accepted
- **Platform:** .NET and Android Kotlin
- **Location:** `sources/FtpsServerLibrary/FtpsCertificateLoader.cs` (`CreateSelfSigned`, 3650 days), `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/Certificates.kt` (`notAfter`, 3650 days)

The generated certificate stays valid for 10 years. Clients trust it by fingerprint, not through a certificate authority. A shorter validity would limit a copied private key only for clients that reject an expired certificate they already trusted, and copying the key requires access to the user profile, which already gives more than the key. Each renewal changes the fingerprint, so every client would have to trust the new one, and the certificate is replaced only on server start, so a server left running past the 7-day margin would serve an expired certificate until restarted. A certificate with 7 days or less left, or one missing a current IPv4 address, is still replaced on the next start. Do not shorten the validity without a renewal that works while the server runs.

## 5. Password comparison is not constant-time

- **Status:** Accepted
- **Platform:** .NET and Android Kotlin
- **Location:** `sources/FtpsServerLibrary/FtpsServerClientSession.cs` (`HandlePassAsync`, `user.Password == password`), `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/FtpsClientSession.kt` (`handlePass`, `found.password == password`)

Both checks use plain string equality, which returns early on a length mismatch or on the first block that differs. The difference cannot be measured from the network. String equality in .NET compares 16 or 32 bytes per vector step, so a password of up to 8-16 characters is compared in one step, and the difference between a match and a mismatch is a few nanoseconds. Android uses a native comparison of the same kind. A remote timing attack has to recover that difference through TLS and network jitter measured in microseconds to milliseconds, which takes a very large number of samples for each guessed position. The login throttle allows 5 failures per address, then a 60 second lockout and a 1 second delay after each failure, so collecting those samples from one address takes years. `FixedTimeEquals` would not change any of this, and the length would still differ in timing. Revisit only if the throttle is removed or passwords are compared somewhere a local caller can time them precisely.

## 11. Symlink validation is check-then-open (TOCTOU)

- **Status:** Accepted
- **Platform:** .NET
- **Location:** `sources/FtpsServerLibrary/IFtpsServerFileSystemProvider.cs` (`GetRealPath`, `ResolveLinks`) and its callers (`FileCreate`, `FileOpenRead`, moves, deletes)

`GetRealPath` checks containment, resolves each link and returns a path string. The file is then opened by that path in a separate step. A process that replaces a directory or file in the share with a symlink or junction between the two steps can redirect the open, or a STOR, outside the share. An FTP client cannot do this, because the server has no command that creates a link. It takes a local process that can write inside the shared folder. That process gains something only if it runs as a different OS account from the server and the server account can reach files it cannot. A process running as the same account already has the same file access. Closing the race needs handle-based I/O on every platform (`openat2` with `RESOLVE_BENEATH` on Linux, handle-relative opens on Windows), which is a large change for this narrow case. Requirement: do not share a folder that an untrusted local account can write to.
