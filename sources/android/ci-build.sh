#!/usr/bin/env bash
# Native Android release APK (GitHub/RuStore), general AAB (Google Play),
# and China PIPL-policy AAB (chinaPiplPolicy flavor) for GitHub Actions.
# Required env: VERSION
# Signing env (same secrets as the previous Avalonia APK):
#   ANDROID_KEYSTORE_FILE, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS, ANDROID_KEY_PASSWORD
set -euo pipefail

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
ANDROID_ROOT="$ROOT/sources/android"
VERSION="${VERSION:?VERSION is required}"
VERSION="$(printf '%s' "$VERSION" | grep -oE '[0-9]{4}\.[0-9]{2}\.[0-9]{2}' | head -1)"
if [ -z "$VERSION" ]; then
  echo "VERSION must contain a date like 2026.08.29 (from CHANGELOG.md)."
  exit 1
fi
VERSION_CODE="${VERSION_CODE:-$(printf '%s' "$VERSION" | tr -d '.')}"
OUT_DIR="${OUT_DIR:-$ROOT/Output}"

mkdir -p "$ANDROID_ROOT/app/src/main/res/drawable"
cp "$ROOT/sources/FtpsServerAvalonia/FtpsServerAvalonia.Android/Icon.png" \
  "$ANDROID_ROOT/app/src/main/res/drawable/ic_launcher.png"
python3 "$ANDROID_ROOT/tools/convert_resx.py"

WRAPPER="$ANDROID_ROOT/gradle/wrapper/gradle-wrapper.jar"
if [ ! -f "$WRAPPER" ]; then
  curl -fsSL "https://raw.githubusercontent.com/gradle/gradle/v8.11.1/gradle/wrapper/gradle-wrapper.jar" -o "$WRAPPER"
fi

# Replace the tiny custom launcher with Gradle's official POSIX wrapper (LF, GRADLE_OPTS-safe).
curl -fsSL "https://raw.githubusercontent.com/gradle/gradle/v8.11.1/gradlew" -o "$ANDROID_ROOT/gradlew"
chmod +x "$ANDROID_ROOT/gradlew"
sed -i 's/\r$//' "$ANDROID_ROOT/gradlew"

unset GRADLE_OPTS || true

(
  cd "$ANDROID_ROOT"
  sh ./gradlew --no-daemon \
    assembleGeneralRelease \
    bundleGeneralRelease \
    bundleChinaPiplPolicyRelease \
    -PappVersionName="$VERSION" \
    -PappVersionCode="$VERSION_CODE"
)

apk=""
if [ -f "$ANDROID_ROOT/app/build/outputs/apk/general/release/app-general-release.apk" ]; then
  apk="$ANDROID_ROOT/app/build/outputs/apk/general/release/app-general-release.apk"
fi
if [ -z "$apk" ]; then
  echo "Signed general release APK not found. ANDROID_KEYSTORE_* secrets may be missing."
  find "$ANDROID_ROOT/app/build/outputs/apk" -name '*.apk' -type f || true
  exit 1
fi

aab=""
if [ -f "$ANDROID_ROOT/app/build/outputs/bundle/generalRelease/app-general-release.aab" ]; then
  aab="$ANDROID_ROOT/app/build/outputs/bundle/generalRelease/app-general-release.aab"
fi
if [ -z "$aab" ]; then
  echo "Signed general release AAB not found. ANDROID_KEYSTORE_* secrets may be missing."
  find "$ANDROID_ROOT/app/build/outputs/bundle" -name '*.aab' -type f || true
  exit 1
fi

china_aab=""
if [ -f "$ANDROID_ROOT/app/build/outputs/bundle/chinaPiplPolicyRelease/app-chinaPiplPolicy-release.aab" ]; then
  china_aab="$ANDROID_ROOT/app/build/outputs/bundle/chinaPiplPolicyRelease/app-chinaPiplPolicy-release.aab"
fi
if [ -z "$china_aab" ]; then
  echo "Signed China PIPL policy AAB not found."
  find "$ANDROID_ROOT/app/build/outputs/bundle" -name '*.aab' -type f || true
  exit 1
fi

mkdir -p "$OUT_DIR"
dest_apk="$OUT_DIR/ftpsserver_${VERSION}_android.apk"
dest_aab="$OUT_DIR/ftpsserver_${VERSION}_android.aab"
dest_china_aab="$OUT_DIR/ftpsserver_${VERSION}_android_china.aab"
cp "$apk" "$dest_apk"
cp "$aab" "$dest_aab"
cp "$china_aab" "$dest_china_aab"
echo "APK: $dest_apk"
ls -lh "$dest_apk"
echo "AAB: $dest_aab"
ls -lh "$dest_aab"
echo "China PIPL AAB: $dest_china_aab"
ls -lh "$dest_china_aab"
