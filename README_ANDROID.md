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
./sources/android/check-local.ps1 -Install
```

The default debug run builds three APKs with different package ids so they can sit on one phone: screenshots (`general`, English-only), general debug, and China PIPL debug (`-ChinaPipl` is not required for that). `-Install` installs all three.

Screenshots version uses English locale and has a different id from the release app.
Debug version will have different id from release app.
