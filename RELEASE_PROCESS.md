# Release process

Put version in

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