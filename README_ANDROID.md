# Android Installation

GitHub releases and RuStore ship a **native Kotlin** app from [`sources/android`](./sources/android) (`com.siarheikuchuk.ftpsserver`, about 4.5 MB).

## Why Kotlin (not Avalonia/.NET)

FOSS repositories such as F-Droid do not accept the Avalonia Android build: they need to compile everything from source on Debian, and they cannot do that for .NET / NuGet / AOT. They also enforce a **30 MB** APK cap. The .NET APK was already close to that limit (~27 MB). Kotlin + Gradle is a few megabytes and is a normal Android source build.

The Avalonia Android project remains at `sources/FtpsServerAvalonia/FtpsServerAvalonia.Android` so the code is not lost, but it is **not** the product Android app anymore. CI, GitHub releases, and RuStore use only the Kotlin app. The same package id and signing keystore are kept, so the Kotlin APK can update an existing Avalonia install.

The repository license is [CC0 1.0](./LICENSE). You may copy the Kotlin FTPS server (and the rest of `sources/android`) into your own projects.

Requires **Android 6.0 (API 23)** or newer. The release APK includes `arm64-v8a`, `armeabi-v7a`, `x86`, and `x86_64`.

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
