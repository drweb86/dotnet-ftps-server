# Native Android FTPS Server (Kotlin)

This is the **shipped** Android app. GitHub releases and RuStore build this tree, not the Avalonia/.NET Android project.

## Why this exists

FOSS stores (F-Droid and similar) reject .NET/Avalonia APKs because they cannot rebuild them from source on Debian, and they reject packages over **30 MB**. This Gradle/Kotlin app is a few megabytes and is a normal Android source build.

`sources/FtpsServerAvalonia/FtpsServerAvalonia.Android` is still in the repository for reference. It is not maintained as the product app.

## Requirements

- minSdk 23 (Android 6.0)
- compileSdk / targetSdk 35 (Android 15)
- JDK 17+
