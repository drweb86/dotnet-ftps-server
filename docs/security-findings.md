# Security findings

Review date: 4 October 2026. Updated after fixes for items 3, 4, 5, 6, and 7.

Items below are still open. Numbers are unchanged. Items 1 and 2 were accepted on purpose and live in `docs/mitigated-security-findings.md`; do not reopen them unless that file says the decision no longer holds. Fixed items are at the end. Odd numbers from the original list are the native Android Kotlin app (`sources/android`). Where the same issue exists in the .NET library or the .NET apps, the next number is that counterpart. The .NET code is `FtpsServerLibrary`, used by the Windows app, the Avalonia app, the console host, and the Avalonia Android view.

File commands in both implementations require a successful TLS login, and paths stay inside the folder configured for that account.

## 8. Cipher suites follow the operating system

- **Severity:** Low
- **Platform:** .NET
- **Location:** `sources/FtpsServerLibrary/FtpsServerClientSession.cs` (line 370, and the same `AuthenticateAsServerAsync` call on each data connection)

The library allows TLS 1.2 and TLS 1.3 and does not set `CipherSuitesPolicy`. Suite selection is whatever the host OS still enables. Current Windows and Linux defaults leave RC4 and 3DES disabled. The Android app refuses those suites itself (item 7, fixed). A machine whose OS policy still allows a weak suite will negotiate it, because this server does not refuse it.

## 9. Account passwords are shown, copied, and shared

- **Severity:** Low
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/ui/ConnectionDetails.kt` (line 64), `MainScreen.kt` (lines 267 and 279), password fields in `MainScreen.kt` (line 436)

The connection card prints each account password. Copy places that text on the clipboard, and Share sends it to another app. The password fields are not masked, and the activity does not set `FLAG_SECURE`, so the recents thumbnail can capture the screen.

## 10. Account passwords are shown and copied

- **Severity:** Low
- **Platform:** .NET
- **Location:** `sources/FtpsServerAppsShared/Helpers/ConnectionDetailsText.cs` (line 129); clipboard copy in `sources/FtpsServerWindows/Controls/ConnectionInstructionControl.xaml.cs` (line 37) and `sources/FtpsServerAvalonia/FtpsServerAvalonia/Controls/ConnectionInstructionControl.axaml.cs` (line 52); share in the Avalonia Android view (`AndroidView.axaml`, `ShowShareButton="True"`)

Windows, Avalonia desktop, and Avalonia Android build the same connection card, including each account password. Copy puts that text on the clipboard. The Avalonia Android view also shares it. The user-password and certificate-password fields are plain text boxes; `PasswordChar` is never set.

## 11. Data connection is tied to the client IP only

- **Severity:** Low
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/FtpsClientSession.kt` (line 417)

Passive mode rejects a data socket whose address differs from the control connection. It does not require the data handshake to resume the control TLS session. A process that shares the client’s source IP can connect first and complete its own handshake. There is no `PORT` or `EPRT` command.

## 12. Data connection is tied to the client IP only

- **Severity:** Low
- **Platform:** .NET
- **Location:** `sources/FtpsServerLibrary/FtpsServerClientSession.cs` (`IsDataPeerFromControlClient`, line 1259)

`AcceptDataClientAsync` compares the normalized control and data addresses and does not check TLS session resumption. The same shared-IP race as item 11 applies. Active mode (`PORT` / `EPRT`) is not implemented here either.

## 13. A write account can delete the shared root

- **Severity:** Low
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/storage/SafFileSystemProvider.kt` (line 69)

`RMD /` resolves to an empty virtual path. `directoryDelete()` then calls `delete()` on the tree URI itself, which removes the folder the user granted. `STOR` of a name that already belongs to a directory also deletes that directory before creating a file. Both require write permission.

## 14. A write account can delete the shared folder

- **Severity:** Low
- **Platform:** .NET
- **Location:** `sources/FtpsServerLibrary/IFtpsServerFileSystemProvider.cs` (`DirectoryDelete`, line 82; `IsInsideBase`, line 275)

An empty virtual path is treated as the user folder: the trimmed path equals the base, so the containment check allows it. `Directory.Delete(actualFolder, recursive: true)` then removes that folder and everything under it. A write-enabled account can do this with `RMD /`. `STOR` onto an existing directory name does not delete the directory; that part of item 13 is specific to the Android Storage Access Framework provider.

## 15. Self-signed name does not match the address clients use

- **Severity:** Low
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/server/Certificates.kt` (line 166)

The generated certificate lists only `127.0.0.1` and `localhost`. The connection card tells the user to connect to the phone’s LAN address, which cannot match that name. Clients warn on every connection. A device on the same network can present a different certificate unless the user compares the SHA-256 fingerprint the card shows.

## 16. Self-signed name omits the LAN address

- **Severity:** Low
- **Platform:** .NET
- **Location:** `sources/FtpsServerLibrary/FtpsCertificateLoader.cs` (`CreateSelfSignedServerCertificate`, line 102)

The generated certificate includes IPv4 and IPv6 loopback, `localhost`, and `Environment.MachineName`. It does not include the LAN addresses the connection card lists. A client that connects by IP still cannot match the name, and the practical check is the SHA-256 fingerprint, as in item 15. Connecting by the machine’s hostname can match, which the Android certificate does not allow.

## 17. A plaintext settings value skips the keystore

- **Severity:** Low
- **Platform:** Android Kotlin
- **Location:** `sources/android/app/src/main/java/com/siarheikuchuk/ftpsserver/security/KeystoreCipher.kt` (line 35)

`decrypt()` returns the stored string unchanged when it does not start with `enc::`. A local writer of `settings.json` can plant a known account password without the hardware-backed key. Other apps cannot write that file. This matters after root, a restored copy of the file, or another bug that can edit it.

## 18. A plaintext settings value skips OS protection

- **Severity:** Low
- **Platform:** .NET
- **Location:** `sources/FtpsServerAppsShared/Security/SecretProtector.cs` (`Unprotect`, line 32); callers in `sources/FtpsServerWindows/Services/SettingsManager.cs` and `sources/FtpsServerAvalonia/FtpsServerAvalonia/Services/SettingsManager.cs`

`Unprotect` returns the value unchanged when it is empty or does not start with `enc::`. Windows (DPAPI), Linux (machine-id key), and the Avalonia Android build (Android Keystore) all use this helper for account passwords and the certificate password. A local writer of the settings file can plant a known password, and the next load accepts it. The normal save path encrypts the value again, so this is a bypass of the protector, not the steady-state storage format.

## Fixed

### 3. Unauthenticated clients can fill the server

- **Platform:** Android Kotlin
- **Was:** Medium

A control connection that sends nothing for 30 seconds before login, or sits idle for 5 minutes after login, is closed. A command line longer than 8192 characters is refused. Simultaneous connections are capped at 100.

### 4. Unauthenticated clients can fill the server

- **Platform:** .NET
- **Was:** Medium

A control connection that sends nothing for 30 seconds before login, or sits idle for 5 minutes after login, is closed. A command line longer than 8192 characters is refused. Simultaneous connections are capped at 100.

### 5. Stop leaves live sessions running

- **Platform:** Android Kotlin
- **Was:** Medium

`stop()` still closes the listening socket, and it also closes every accepted control socket, its passive listener, and a data socket already in a transfer.

### 6. Stop leaves live sessions running

- **Platform:** .NET
- **Was:** Medium

`Stop()` still stops the listener, and it also aborts each live session: the control connection, the passive listener, and a data connection already in a transfer.

### 7. Cipher suites follow the platform default

- **Platform:** Android Kotlin
- **Was:** Medium

Control and data sockets enable TLS 1.2 and TLS 1.3 only, and only forward-secret AEAD suites. RC4, 3DES, CBC, and RSA key transport are left disabled. Item 8 is the .NET counterpart and is still open.
