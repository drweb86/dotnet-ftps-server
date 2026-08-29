# Android APK Signing

Release APKs are the native Kotlin app in `sources/android`, built by GitHub Actions and signed with the same self-signed Android keystore as earlier Avalonia APKs. The Avalonia Android project is no longer what CI publishes.
Keep that keystore for future releases, otherwise Android will reject app updates installed over an earlier version.

## Create a self-signed keystore

Run this once on a machine with a JDK installed:

```bash
keytool -genkeypair \
  -v \
  -keystore ftpsserver-release.keystore \
  -alias ftpsserver \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

Choose strong passwords when prompted. The key password may be the same as the keystore password.

## Add GitHub Actions secrets

Encode the keystore file:

```bash
base64 -w 0 ftpsserver-release.keystore
```

On Windows PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("ftpsserver-release.keystore"))
```

Add these repository secrets:

- `ANDROID_KEYSTORE_BASE64`: the base64 text from the command above.
- `ANDROID_KEYSTORE_PASSWORD`: the keystore password.
- `ANDROID_KEY_ALIAS`: `ftpsserver`, unless you chose another alias.
- `ANDROID_KEY_PASSWORD`: the key password.

The GitHub release and RuStore get a single APK:

- `ftpsserver_<version>_android.apk`

The package id is `com.siarheikuchuk.ftpsserver`. Because the signing key is unchanged, this APK can update an existing install of the older Avalonia build.
