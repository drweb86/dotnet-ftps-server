# Android APK Signing

Release APKs are signed by GitHub Actions with a self-signed Android keystore.
Keep the same keystore for future releases, otherwise Android will reject app updates installed over an earlier version.

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

The workflow publishes the signed APK as `ftpsserver_<version>_android.apk`.
