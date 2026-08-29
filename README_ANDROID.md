# Android Installation

GitHub releases and RuStore ship the native Kotlin app from `sources/android` (`com.siarheikuchuk.ftpsserver`, ~13 MB). It is signed with the same keystore as the older Avalonia APK, so it can update an existing install.

The Avalonia/.NET project in `sources/FtpsServerAvalonia/FtpsServerAvalonia.Android` remains in the tree but is no longer built by CI.

To build and optionally install a debug APK on a phone or emulator:

```powershell
powershell -File sources/android/check-local.ps1
powershell -File sources/android/check-local.ps1 -Install
```

Debug builds use application id `com.siarheikuchuk.ftpsserver.debug` so they can sit next to the store app.

Best way via Application Store:
- [RuStore](https://www.rustore.ru/catalog/app/com.siarheikuchuk.ftpsserver)  

Manual process:

The Android APK is self-signed and published as a release asset named:

```text
ftpsserver_<version>_android.apk
```

Because it is self-signed and not installed from Google Play, Android will ask you to confirm that you trust the APK before installing it.

## Install From Phone Browser

1. Open the latest GitHub release on your Android device:
   https://github.com/drweb86/dotnet-ftps-server/releases/latest
2. Download `ftpsserver_<version>_android.apk`.
3. If the browser warns that APK files can be harmful, continue only if the file was downloaded from the project release page.
4. Tap the downloaded APK in the browser download list, or open it from the Android `Downloads` app.
5. If Android blocks the install, tap `Settings` and enable `Allow from this source` for the browser or file manager you used.
6. Go back and tap `Install`.
7. If Google Play Protect shows a warning for the self-signed APK, choose the option to install anyway if you trust this project. On Samsung devices click Details, Install anyway.

## Updating

Install a newer `ftpsserver_<version>_android.apk` over the existing app.

Android requires all updates for the same app to be signed with the same signing key. If Android says the package conflicts with an existing app or the signature does not match, uninstall the old app first, then install the new APK.

## After Installation

Start FTPS Server, add at least one user, choose a shared folder, and tap `Start`. While the server is running, a notification stays on screen and the app keeps the CPU and Wi-Fi awake so transfers continue with the screen off.
