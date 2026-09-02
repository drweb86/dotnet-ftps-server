# Native Android FTPS Server (Kotlin)

This is the **shipped** Android app. GitHub releases and RuStore build this tree, not the Avalonia/.NET Android project.

## Why this exists

FOSS stores (F-Droid and similar) reject .NET/Avalonia APKs because they cannot rebuild them from source on Debian, and they reject packages over **30 MB**. This Gradle/Kotlin app is a few megabytes and is a normal Android source build.

`sources/FtpsServerAvalonia/FtpsServerAvalonia.Android` is still in the repository for reference. It is not maintained as the product app.

## Requirements

- minSdk 23 (Android 6.0)
- compileSdk / targetSdk 36 (Android 16)
- JDK 17+

## Product flavors

- `general` (default) — Privacy menu shows the embedded policy (OK to dismiss). No consent gate.
- `chinaPiplPolicy` — first-launch Agree/Disagree gate, persisted consent, withdraw from Privacy, wipe of app-private data only.

Local debug (three APKs): `./check-local.ps1` then optional `-Install`.
Local China-only release: `./check-local.ps1 -Release -ChinaPipl`.
CI AAB: `ftpsserver_<version>_android_china.aab`.
F-Droid / default store builds should use `assembleGeneralRelease`.
