# Native Android FTPS Server (Kotlin)

This is the **shipped** Android app. GitHub releases and RuStore build this tree, not the Avalonia/.NET Android project.

## Why this exists

FOSS stores (F-Droid and similar) reject .NET/Avalonia APKs because they cannot rebuild them from source on Debian, and they reject packages over **30 MB**. This Gradle/Kotlin app is a few megabytes and is a normal Android source build.

`sources/FtpsServerAvalonia/FtpsServerAvalonia.Android` is still in the repository for reference. It is not maintained as the product app.

## License / reuse

The repository is [CC0 1.0](../../LICENSE). Copy the Kotlin FTPS server under `app/src/main/java/com/siarheikuchuk/ftpsserver/server/` (and the SAF storage adapter) into your own project if you want. No extra permission is required.

## Layout

- `app/` — Compose UI, foreground service, SAF file access, FTPS protocol
- `tools/convert_resx.py` — copies strings from the Avalonia `.resx` files
- `tools/generate_fastlane.py` — writes `fastlane/metadata/android` from Windows `.resx` translations
- `check-local.ps1` / `check-local.sh` — local debug/release build (`-Screenshots` / `--screenshots` for an English-only APK used to capture store screenshots)
- `ci-build.sh` — signed release APK for GitHub Actions

## Requirements

- minSdk 23 (Android 6.0)
- compileSdk / targetSdk 35 (Android 15)
- JDK 17+
