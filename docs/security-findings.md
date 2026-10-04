# Security findings

Review date: 4 October 2026. Updated after fixes for items 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16, 17, and 18.

Items below are still open. Numbers are unchanged. Items 1 and 2 were accepted on purpose and live in `docs/mitigated-security-findings.md`; do not reopen them unless that file says the decision no longer holds. Fixed items are at the end. Odd numbers from the original list are the native Android Kotlin app (`sources/android`). Where the same issue exists in the .NET library or the .NET apps, the next number is that counterpart. The .NET code is `FtpsServerLibrary`, used by the Windows app, the Avalonia app, the console host, and the Avalonia Android view.

File commands in both implementations require a successful TLS login, and paths stay inside the folder configured for that account.

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

### 13. A write account can delete the shared root

- **Platform:** Android Kotlin
- **Was:** Low

`RMD /` and a rename of the granted folder are refused. Uploading a file onto a directory name fails instead of deleting that directory.

### 14. A write account can delete the shared folder

- **Platform:** .NET
- **Was:** Low

`RMD /` and a rename of the user folder are refused, including when a link inside the share points back at that folder. Deleting a directory that is inside the share still works.

### 11. Data connection is tied to the client IP only

- **Platform:** Android Kotlin
- **Was:** Low

A protected data connection must resume a TLS session already cached for this server. A new handshake is refused, and the source address must still match the control connection. TLS 1.2 must present that control connection's session id. TLS 1.3 uses a new session id on the resumed connection, so a resumed TLS 1.3 session is accepted after session creation is disabled.

### 12. Data connection is tied to the client IP only

- **Platform:** .NET
- **Was:** Low

A protected data connection must resume the control TLS session, and the source address must still match. TLS 1.2 must present that control connection's session id. On Windows, TLS 1.3 assigns a new session id to the resumed connection, so the server accepts it only when Schannel marks the handshake as a reconnect. A full handshake is refused. On Android, the runtime does not expose the session id, so the address check remains.

### 15. Self-signed name does not match the address clients use

- **Platform:** Android Kotlin
- **Was:** Low

The generated certificate includes the phone's current IPv4 addresses, plus `127.0.0.1` and `localhost`. It is replaced when one of those addresses is missing or the certificate expires within 7 days. The fingerprint on the connection card changes when the certificate is replaced. A certificate the user supplied is left as it is. Item 16 is the .NET counterpart and is still open.

### 17. A plaintext settings value skips the keystore

- **Platform:** Android Kotlin
- **Was:** Low

Once the keystore key exists, a settings password without the `enc::` prefix is refused and the file is left unchanged. Before that key exists, a password stored by an older version is still read and then encrypted. Settings encryption starts in this release, so that first read is how an existing install is upgraded.

### 18. A plaintext settings value skips OS protection

- **Platform:** .NET
- **Was:** Low

Windows and Linux still accept a plaintext settings password. Avalonia Android refuses one once the keystore key exists, and leaves the file unchanged, same as item 17. A password stored before that key exists is still read and then encrypted.

### 8. Cipher suites follow the operating system

- **Platform:** .NET
- **Was:** Low

Control and data connections allow only forward-secret AEAD suites with TLS 1.2 and TLS 1.3. RC4, 3DES, CBC, and RSA key transport are refused. Linux and macOS offer only those suites. Windows and Android cannot set that list, so the server closes the connection when the negotiated suite is not one of them.

### 16. Self-signed name omits the LAN address

- **Platform:** .NET
- **Was:** Low

The generated certificate includes the machine's current IPv4 addresses, plus loopback, `localhost`, and the machine name. It is replaced when one of those IPv4 addresses is missing or the certificate expires within 7 days. The fingerprint changes when the certificate is replaced. A certificate the caller supplied is left as it is. The listener is IPv4 only, so IPv6 addresses are not added.
