## Screenshots

Phone:

<img src="fastlane/metadata/android/en-US/images/phoneScreenshots/1.png" width="240" alt="Phone: start the server" /><img src="fastlane/metadata/android/en-US/images/phoneScreenshots/2.png" width="240" alt="Phone: running server and certificate" /><img src="fastlane/metadata/android/en-US/images/phoneScreenshots/3.png" width="240" alt="Phone: users and shared folder" />

Tablet:

<img src="fastlane/metadata/android/en-US/images/tenInchScreenshots/1.jpg" width="420" alt="Tablet: start the server" />
<img src="fastlane/metadata/android/en-US/images/tenInchScreenshots/2.jpg" width="420" alt="Tablet: running server and certificate" />
<img src="fastlane/metadata/android/en-US/images/tenInchScreenshots/3.jpg" width="420" alt="Tablet: users and server logs" />

## Installation

Stores:
- [RuStore](https://www.rustore.ru/catalog/app/com.siarheikuchuk.ftpsserver)  

Manually:
- Release contains prebuilt asset `ftpsserver_<version>_android.apk`.
Tap the downloaded APK in the browser download list, or open it from the Android `Downloads` app. If Android blocks the install, tap `Settings` and enable `Allow from this source` for the browser or file manager you used, go back and Install.

Because app is self-signed, Android will ask you to confirm that you trust the APK before installing it. When Google Play Protect shows a warning for the self-signed APK, choose the option to install anyway if you trust this project. On Samsung devices click Details, Install anyway.

## Updating

If you install from store, store will update application. Otherwise you can go to releases and do it mnanually. Update information will be shown in application.

Android requires all updates for the same app to be signed with the same signing key. If Android says the package conflicts with an existing app or the signature does not match, uninstall the old app first, then install the new APK.

## Usage

Start FTPS Server, add at least one user, choose a shared folder, and tap `Start`. While the server is running, a notification stays on screen and the app keeps the CPU and Wi-Fi awake so transfers continue with the screen off. Once you done your transfers, you must stop server.

## Source code

There're 2 implementations:

a. Dotnet 10 Avalonia based

This is historically first version. However FOSS stores do not approve dotnet based apps and those which size are above 30MB. So this version is still there for your own needs to create implementation, but it won't be deployed. So its workable version.

b. Kotlin app (Current)

This is current version.

Build in Powershell:

```powershell
./sources/android/check-local.ps1
./sources/android/check-local.ps1 -Screenshots
./sources/android/check-local.ps1 -Install
Screenshots
```

Screenshots version will not check for updates and use english locale and have different id from release app.
Debug version will have different id from release app.
