# Release process

When a release build fails and you need a new version with the same notes:

```powershell
powershell -File scripts/patch-version.ps1
```

That increments the last number in the CHANGELOG heading (`2026.09.19` → `2026.09.20`), updates both Gradle literals, writes the notes under that heading into the Fastlane en-US changelog (plain text, under 500 bytes), and renames the Fastlane file to the new `versionCode`. Then continue from **d**.

Otherwise put the version in by hand:

a. [`CHANGELOG.md`](./CHANGELOG.md) into first line prefixed with '# ':

```markdown
# 2026.09.01
```

b. [`sources/android/app/build.gradle.kts`](./sources/android/app/build.gradle.kts) set both literals to that version:

```kotlin
val defaultVersionCode = 20260901
val defaultVersionName = "2026.09.01"
```

c. Rename changelog (en-US is enough; other locales fall back to it):

`fastlane/metadata/android/en-US/changelogs/20260901.txt`

Filename is `versionCode` with no dots. Plain text, under 500 bytes.

d. Push.

e. Create release with create tag matching version.

f. Submit automatically created PR in win-get form to win-get.

g. Submit library to NUGET (only if it is changed).

h. F-Droid setup

```
After a tag that includes the Gradle literals and Fastlane exists:

1. Sign in at [gitlab.com](https://gitlab.com) and fork [fdroiddata](https://gitlab.com/fdroid/fdroiddata).
2. Copy [`sources/android/fdroid/com.siarheikuchuk.ftpsserver.yml`](./sources/android/fdroid/com.siarheikuchuk.ftpsserver.yml) to `metadata/com.siarheikuchuk.ftpsserver.yml` on a branch named `com.siarheikuchuk.ftpsserver`.
3. Set `versionName`, `versionCode`, `commit`, `CurrentVersion`, and `CurrentVersionCode` to that tag.
4. Set `AllowedAPKSigningKeys` from the signed APK:

   ```bash
   apksigner verify --print-certs ftpsserver_<version>_android.apk
   ```

   Use the SHA-256 fingerprint, lowercase, no colons.
5. Open a merge request titled `New app: FTPS Server`. Answer packager questions yourself.

After F-Droid accepts the app, new GitHub tags that follow **Every release** above are picked up automatically.
```

i. Microsoft Store (MSIX)

The GitHub Release includes unsigned `ftpsserver_<version>_windows_x64.msix` and `ftpsserver_<version>_windows_arm64.msix`. They are not a replacement for the NSIS setup. The Store listing is [FTPS Server](https://apps.microsoft.com/detail/9PHPG7B75S0T) (Store ID `9PHPG7B75S0T`). Microsoft re-signs the packages after certification.

1. In Partner Center, open that product and start a new submission.
2. Upload both architecture packages (or a bundle if you create one).
3. Declare the restricted capability **runFullTrust**.
4. Privacy policy URL: the desktop policy at [`privacy/desktop/README.md`](./privacy/desktop/README.md) on the default branch.
5. Notes for certification: this is a local FTPS server. The user chooses listen ports and folder roots. A self-signed TLS certificate may be stored under `%LOCALAPPDATA%\FtpsServerLibrary\Certificates`. Requires Windows 11 (build 26100 or later). No ads, no in-app purchases.
6. Submit. Store (MSIX) and GitHub/winget (NSIS) installs do not upgrade each other.