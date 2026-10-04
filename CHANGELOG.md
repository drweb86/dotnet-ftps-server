# 2026.10.04
(unpublished)

## Bug Fixes
- Library: truncate an existing file on upload so a shorter STOR does not keep the previous contents.
- Windows, Avalonia: the GitHub update check decompresses the response encoding it receives, including an uncompressed body.

## Security Hardening
- Android: the control connection is closed when the TLS handshake after AUTH TLS fails, as the .NET server already does. Before, the session went on in cleartext.
- Library, Android: control characters such as ESC and BEL in client text are replaced with `?` in replies and in the log. A client can no longer put terminal escape sequences into a log viewed in a terminal.
- Library, Android: a resumed data connection must now present the control connection's own TLS session id wherever the platform exposes a stable one — TLS 1.2 everywhere and TLS 1.3 on OpenSSL (Linux). A client on the same address that resumes a different session, for example its own, is refused there. Windows and Android keep accepting any resumed TLS 1.3 session: Schannel and Conscrypt report a fresh session id for a resumed TLS 1.3 connection, so no per-session value survives to compare (verified on Windows 11 with .NET 10). The peer-address check still applies on every platform. Android also no longer accepts two sessions that merely share a creation timestamp; millisecond equality is not proof of the same session.
- Library: path containment now respects the platform's case sensitivity. On Linux a symlink inside the share whose target matches the share path only by letter case (for example /srv/FTP when the share is /srv/ftp) was treated as inside the share; it is refused now. Windows keeps case-insensitive comparison.
- Library, Android: data connections are now bounded in time. A transfer command fails when no data connection arrives within 30 seconds, the TLS handshake on the data connection must complete within 30 seconds, and a transfer that moves no bytes for 60 seconds is aborted. An authenticated account can no longer hold every connection slot by opening transfers it never uses. The TLS handshake on the control connection is bounded by 30 seconds too.
- Android, Windows, Avalonia: mask the account password and the certificate password until the user chooses Show. On Android, the recent-apps thumbnail no longer captures the screen. The connection card, Copy, and Share still include the password, because that text is the setup guide.
- Library: allow only forward-secret AEAD cipher suites with TLS 1.2 and TLS 1.3. RC4, 3DES, CBC, and RSA key transport are refused. On Linux and macOS the server offers only those suites. On Windows and Android the operating system still chooses the suite, and the server closes the connection when the result is not one of those suites.
- Library: the generated certificate includes the machine's current IPv4 addresses, and it is replaced when one of those addresses is missing. The fingerprint changes when that happens. The server listens on IPv4, so IPv6 addresses are not added.
- Android: once the keystore key exists, a settings password that is not encrypted is refused and the file is left unchanged. A password saved by an older version is still read on the first start, before that key exists, and is encrypted then.
- Avalonia Android: same rule for settings passwords. Windows and Linux still accept a plaintext password.
- Library, Android: a protected data connection must resume the control connection's TLS session. A new handshake from another process on the same address is refused. The source address must still match.
- Android: the generated certificate includes the phone's current IPv4 addresses, and it is replaced when one of those addresses is missing. The fingerprint on the connection card changes when that happens.
- Library, Android: refuse to delete or move the shared folder. `RMD /` no longer removes the folder an account is rooted at. On Android, uploading a file onto a directory name no longer deletes that directory.
- Android: on the control and data connections, allow only forward-secret AEAD cipher suites with TLS 1.2 and TLS 1.3. RC4, 3DES, CBC, and RSA key transport are disabled.
- Android: close a control connection that sends nothing for 30 seconds before login, or sits idle for 5 minutes after login, and refuse a command line longer than 8192 characters. Simultaneous connections are capped at 100. An unauthenticated client can no longer hold every slot indefinitely or grow one command without a bound.
- Library, Android: stopping the server closes every open control and data connection. A transfer that has already started no longer keeps reading or writing the shared folder after Stop.
- Library: close a control connection that sends nothing for 30 seconds before login, or sits idle for 5 minutes after login, and refuse a command line longer than 8192 characters. Simultaneous connections are capped at 100. An unauthenticated client can no longer hold every slot indefinitely or grow one command without a bound.
- Android: the running server reads accounts from saved settings instead of the service start intent. Passwords are no longer visible in the activity manager, and a restarted service keeps the saved port and accounts.
- Android: store the generated certificate password with the Android Keystore key. An existing `Self-Signed.pfx` that still uses the password `test` is rewritten on the next start, and the certificate stays the same. A copied certificate file no longer opens with `test`.
- Avalonia Android: encrypt FTP passwords and the certificate password in `settings.json` with an Android Keystore key, using the same rules as the Kotlin app. A copied settings file cannot be read on another device, and an edited file is rejected. Passwords saved by an older version are encrypted the next time the app starts. If decryption fails, later saves do not replace the file.
- Library: on Android, store the auto-generated certificate password with that Keystore key. An existing `Self-Signed.pfx` that still uses the password `test` is rewritten on the next start, and the certificate stays the same.
- Android: encrypt FTP passwords and the certificate password in `settings.json` with an Android Keystore key. A copied settings file cannot be read on another device, and an edited file is rejected. Passwords saved by an older version are encrypted the next time the app starts. If decryption fails, the existing file is left in place.
- Android: release builds allow no cleartext HTTP and trust only system certificate authorities. A user-installed CA cannot read the app's own connections, and the unused emulator exception for `10.0.2.2` stays in debug builds only.
- Android: exclude app-private files from cloud backup and phone-to-phone transfer. `settings.json` and the server certificate are not copied to the user's Google account or onto another device. `allowBackup="false"` was already set; Android 12 and later also need these extraction rules, because device transfer can still run when that flag is off.
- Avalonia Android: turn backup and cleartext traffic off, and apply the same network and extraction rules. The manifest previously left backup at the platform default (on), and its network security config was not packaged, so a user-installed CA and Auto Backup could both reach app data.
- Library, Android: reject Windows reserved device names that include an extension, such as `con.txt`. On Windows, open share paths with the `\\?\` prefix so those names are files, not devices.
- Android: allow only TLS 1.2 and TLS 1.3 on the control and data connections.
- Library, Android: after 5 failed logins from one address, pause one second on each failure and refuse that address for 60 seconds. A successful login clears the count.
- Library: on Windows and Linux, store the auto-generated certificate password with an OS key. An existing `Self-Signed.pfx` that still uses the password `test` is rewritten on the next start. macOS keeps `test`.
- Library, Android: send a fixed error line to the FTP client. The exception, including the real path, stays in the log.
- Android: reject a `..` segment in the storage-picker folder walk instead of moving to the parent document.
- Library: reject a symlink or junction inside the share when its target is outside the user folder. Those links are also omitted from directory listings. A link that stays inside the share is still listed and can be opened.
- Console app does not allow to save account passwords in plaintext.
- Some tests were added to address security-critical code.

## New Features
- Support for Linux (not Ubuntu only)

## Changes
- Console, Windows, Avalonia: NLog 6.2.1 and Avalonia 12.1.3. The third-party notices list those versions.

# 2026.09.36

## Changes
- Android: Rebuild the release APK so F-Droid can match classes.dex and resources.arsc.

# 2026.09.35

## Changes
- Android: Another attempt to fix F-Droid build.

# 2026.09.33

## Changes
- Android: build the release APK with JDK 21.0.12.1 and the committed string resources so F-Droid can reproduce it.

## Bug Fixes
- Windows: follow Settings → Accessibility → Text size, and refresh fonts when the window moves to a display with a different scale.

# 2026.09.29

## Security
- Windows, Linux: encrypt FTPS account passwords and the certificate password in `settings.json` (DPAPI current-user on Windows, machine-bound AES on Linux). Passwords saved by an older version are encrypted the next time the app starts.

# 2026.09.26

## Changes
- Documents.

# 2026.09.21

## Changes
- Android: disable ART baseline profiles so F-Droid can reproduce the GitHub APK.
- Fixup ASO complaints.

# 2026.09.20

## Security
- Library, Android: require `AUTH TLS` before `USER`/`PASS` (and `PBSZ`/`PROT`) whenever a certificate is loaded, so a plain-FTP client cannot send the password on a clear control channel. Reported by [gitubpatrice](https://gitlab.com/gitubpatrice).
- Library, Android: reject a PASV data connection whose peer IP does not match the control connection, and bind the passive listener to that interface. Reported by [gitubpatrice](https://gitlab.com/gitubpatrice).

## Changes
- UI: Embed localized CC0 license text in Windows, Avalonia, and Android apps (no outbound license link).
- Android (China edition): require license agreement before use, allow withdrawal, and append China-market terms (developer identity, PRC law, no paid services, uninstall).
- Android: remove leftover in-app update strings from APK resources.
- UI: do not prefill `userN` / `passwordN` when adding an account.
- Windows Store (MSIX): do not check GitHub for updates; the Store delivers updates for that install.
- UI: Update texts for Windows Store package.

# 2026.09.14

## Changes
- UI: Add privacy documents to desktop apps.
- UI: Preparation for Microsoft Store.

# 2026.09.12

## Changes
- UI: Improve user experience.
- Update some libraries.

# 2026.09.06

## Changes
- Android: privacy policy names the app **FTPS Server** and the developer **Siarhei Kuchuk**.
- Android: launcher and in-app titles use **FTPS Server** in every language.

# 2026.09.04

## Changes
- Android: target API 36 (Google Play requirement).

# 2026.09.03

## Changes
- Android: removed in-app update checks and website / license links (store listing rules).
- Android: in-app Privacy policy (embedded, no links) (store listing rules).

# 2026.08.36

## Changes
- Android: Preparation for F-Droid, Google Store.
- UI: Possibility to share connection details over chat.

## Bug Fixes
- Android: Application was spawning other instances.

# 2026.08.31

## New Features
- Windows: Win-get support for all application languages (app is still under review process, so delivery will be skipped).

## Changes
- Android: the shipped app is now native Kotlin (`sources/android`) instead of Avalonia/.NET, so FOSS stores can build from source and stay under the 30 MB APK cap.
- Android: GitHub/RuStore APK uses the same package id and signing key as before, so it can update an existing install.
- Android: align `ApplicationId` with the RuStore package `com.siarheikuchuk.ftpsserver`.

# 2026.08.27

## Bug Fixes
- Android: build is unsighed.

# 2026.08.26

## New Features
- Library: MLSD and MLST listings (RFC 3659) include a full UTC timestamp (`yyyyMMddHHmmss`: year, time, and seconds).

## Changes
- UI: During work, sleep is prevented.
- Update libraries

## Bug Fixes
- Library: LIST dates for older than current year did not include year (!).
- Windows: system hidden folders from now on are excluded from returning by library to handle case when user shared entire hard drive.

# 2026.07.18

## Changes
- UI: Update some libraries.
- UI, console: Better handling of self-signed certificates.

# 2026.06.12

## Changes
- Android app is self-signed. Previously it was preventing installation of it to Android.
- Naming of build artefacts was improved.

## Bug Fixes
- Android: prevent sleep during server running.

# 2026.05.31

## New Features
- Android app.

## Changes
- UI: Update some libraries.

# 2026.05.19

## New Features
- UI: add more languages.

## Changes
- UI: Update some libraries.

# 2026.04.20

## Bug Fixes
- Library: On non-english locales dates were recorded encorrectly in List command, so users might see empty folders.
- Fix application crash on F12 press.
- Ubuntu: for specified certificate UI was not refreshing checkbox.

## New Features
- Library: Possibility to implement own file system.
- UI: Add some languages.
- UI: add github actions for publishing

## Changes
- Library: code simplification.
- Upgrade Avalonia to V12.
- Library will fail on attempt of unencrypted transfer.

# 2025.01.11

## New Features
- Localized to Russian, Spanish (Español), Chinese Simplified (简体中文), German (Deutsch), Japanese (日本語), Portuguese Brazilian (Português do Brasil), Korean (한국어) languages.

# 2025.12.26

## New Features

- Linux: Add UI application.

# 2025.12.25

## New Features

- Library, Console App: add Ubuntu support (and installation script).
- Windows App: add NLogs.
- Windows App: check for updates, possibility to get them.
- Console App: add interactive configuration with possibility
to save configuration into file so user might execute console
and then manually input configuration and have it saved for future use.
To run in interactive mode, simply launch console application without arguments.

## Changes

- Windows App: default directory is desktop.
- Console App: default directory is desktop.

# 2025.12.21

Initial version of FTPS Server UI for Windows on WPF platform.

# 2025.12.2

## New Features
- Library: Add compatibility with FluentFtp

# 2025.11.22

## New Features
- Library: First release to windows.
