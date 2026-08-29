#!/usr/bin/env bash
# Native Android release APK for GitHub Actions.
# Required env: VERSION
# Signing env (same secrets as the previous Avalonia APK):
#   ANDROID_KEYSTORE_FILE, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS, ANDROID_KEY_PASSWORD
set -euo pipefail

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
ANDROID_ROOT="$ROOT/sources/android"
VERSION="${VERSION:?VERSION is required}"
VERSION_CODE="${VERSION_CODE:-$(echo "$VERSION" | tr -d '.')}"
OUT_DIR="${OUT_DIR:-$ROOT/Output}"

mkdir -p "$ANDROID_ROOT/app/src/main/res/drawable"
cp "$ROOT/sources/FtpsServerAvalonia/FtpsServerAvalonia.Android/Icon.png" \
  "$ANDROID_ROOT/app/src/main/res/drawable/ic_launcher.png"
python3 "$ANDROID_ROOT/tools/convert_resx.py"

WRAPPER="$ANDROID_ROOT/gradle/wrapper/gradle-wrapper.jar"
if [ ! -f "$WRAPPER" ]; then
  curl -L "https://github.com/gradle/gradle/raw/v8.11.1/gradle/wrapper/gradle-wrapper.jar" -o "$WRAPPER"
fi

chmod +x "$ANDROID_ROOT/gradlew"
(
  cd "$ANDROID_ROOT"
  ./gradlew --no-daemon assembleRelease \
    -PappVersionName="$VERSION" \
    -PappVersionCode="$VERSION_CODE"
)

apk=$(find "$ANDROID_ROOT/app/build/outputs/apk/release" -name 'app-release.apk' -type f | head -1)
if [ -z "$apk" ]; then
  echo "Signed app-release.apk not found. ANDROID_KEYSTORE_* secrets may be missing."
  find "$ANDROID_ROOT/app/build/outputs/apk" -name '*.apk' -type f || true
  exit 1
fi

mkdir -p "$OUT_DIR"
dest="$OUT_DIR/ftpsserver_${VERSION}_android.apk"
cp "$apk" "$dest"
echo "APK: $dest"
ls -lh "$dest"
