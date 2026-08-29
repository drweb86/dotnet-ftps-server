#!/usr/bin/env sh
# Native Android FTPS Server — local build and optional device install.
# Usage: ./sources/android/check-local.sh [--install] [--release]
set -e
ANDROID_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_ROOT=$(CDPATH= cd -- "$ANDROID_ROOT/../.." && pwd)

find_java() {
  if [ -n "$JAVA_HOME" ] && [ -x "$JAVA_HOME/bin/java" ]; then
    echo "$JAVA_HOME"
    return
  fi
  for c in \
    "/usr/lib/jvm/java-21-openjdk-amd64" \
    "/usr/lib/jvm/java-17-openjdk-amd64"
  do
    if [ -x "$c/bin/java" ]; then echo "$c"; return; fi
  done
  echo "JDK 17+ not found. Set JAVA_HOME." >&2
  exit 1
}

find_sdk() {
  if [ -n "$ANDROID_HOME" ] && [ -d "$ANDROID_HOME/platforms" ]; then echo "$ANDROID_HOME"; return; fi
  if [ -n "$ANDROID_SDK_ROOT" ] && [ -d "$ANDROID_SDK_ROOT/platforms" ]; then echo "$ANDROID_SDK_ROOT"; return; fi
  for c in "$HOME/Android/Sdk" "$HOME/Library/Android/sdk"; do
    if [ -d "$c/platforms" ]; then echo "$c"; return; fi
  done
  echo "Android SDK not found. Set ANDROID_HOME." >&2
  exit 1
}

JAVA_HOME=$(find_java)
export JAVA_HOME
SDK=$(find_sdk)
export ANDROID_HOME="$SDK"
export ANDROID_SDK_ROOT="$SDK"
export PATH="$JAVA_HOME/bin:$SDK/platform-tools:$PATH"

printf 'sdk.dir=%s\n' "$SDK" > "$ANDROID_ROOT/local.properties"
mkdir -p "$SDK/licenses"
printf '24333f8a63b6825ea9c5514f83c2829b004d1fee\n' > "$SDK/licenses/android-sdk-license"

ICON_SRC="$PROJECT_ROOT/sources/FtpsServerAvalonia/FtpsServerAvalonia.Android/Icon.png"
ICON_DST="$ANDROID_ROOT/app/src/main/res/drawable/ic_launcher.png"
if [ -f "$ICON_SRC" ]; then cp "$ICON_SRC" "$ICON_DST"; fi
if command -v python3 >/dev/null; then python3 "$ANDROID_ROOT/tools/convert_resx.py"; fi

WRAPPER="$ANDROID_ROOT/gradle/wrapper/gradle-wrapper.jar"
if [ ! -f "$WRAPPER" ]; then
  curl -L "https://github.com/gradle/gradle/raw/v8.11.1/gradle/wrapper/gradle-wrapper.jar" -o "$WRAPPER"
fi

TASK=assembleDebug
INSTALL=0
for arg in "$@"; do
  case "$arg" in
    --release) TASK=assembleRelease ;;
    --install) INSTALL=1 ;;
  esac
done

chmod +x "$ANDROID_ROOT/gradlew"
(cd "$ANDROID_ROOT" && ./gradlew --no-daemon "$TASK")

APK=$(find "$ANDROID_ROOT/app/build/outputs/apk" -name "*.apk" | head -1)
echo "APK: $APK"
ls -lh "$APK"

if [ "$INSTALL" -eq 1 ]; then
  if ! adb devices | grep -q $'\tdevice$'; then
    echo "No Android device/emulator connected."
    exit 0
  fi
  adb install -r "$APK"
  PKG=com.siarheikuchuk.ftpsserver.debug
  [ "$TASK" = assembleRelease ] && PKG=com.siarheikuchuk.ftpsserver
  adb shell am start -n "$PKG/com.siarheikuchuk.ftpsserver.MainActivity"
fi

echo OK
