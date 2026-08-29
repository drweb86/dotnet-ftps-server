# 2026.08.28
(unpublished)

## New Features
- Windows: Win-get support for all application languages.

## Changes
- Android: align `ApplicationId` with the RuStore package `com.siarheikuchuk.ftpsserver`.
- Android: CI also builds arm64-only and x64-only APKs as workflow artifacts for F-Droid-compatible stores; the GitHub release and RuStore keep a single universal APK.

# 2026.08.27

## Bug Fixes
- Android: build is unsighed.

# 2026.08.26

## New Features
- Library: MLSD and MLST listings (RFC 3659) include a full UTC timestamp (`yyyyMMddHHmmss`: year, time, and seconds).

## Changes
- UI: During work, sleep is prevented.
- Update libraries

## Bug Fixes
- Library: LIST dates for older than current year did not include year (!).
- Windows: system hidden folders from now on are excluded from returning by library to handle case when user shared entire hard drive.

# 2026.07.18

## Changes
- UI: Update some libraries.
- UI, console: Better handling of self-signed certificates.

# 2026.06.12

## Changes
- Android app is self-signed. Previously it was preventing installation of it to Android.
- Naming of build artefacts was improved.

## Bug Fixes
- Android: prevent sleep during server running.

# 2026.05.31

## New Features
- Android app.

## Changes
- UI: Update some libraries.

# 2026.05.19

## New Features
- UI: add more languages.

## Changes
- UI: Update some libraries.

# 2026.04.20

## Bug Fixes
- Library: On non-english locales dates were recorded encorrectly in List command, so users might see empty folders.
- Fix application crash on F12 press.
- Ubuntu: for specified certificate UI was not refreshing checkbox.

## New Features
- Library: Possibility to implement own file system.
- UI: Add some languages.
- UI: add github actions for publishing

## Changes
- Library: code simplification.
- Upgrade Avalonia to V12.
- Library will fail on attempt of unencrypted transfer.

# 2025.01.11

## New Features
- Localized to Russian, Spanish (Español), Chinese Simplified (简体中文), German (Deutsch), Japanese (日本語), Portuguese Brazilian (Português do Brasil), Korean (한국어) languages.

# 2025.12.26

## New Features

- Linux: Add UI application.

# 2025.12.25

## New Features

- Library, Console App: add Ubuntu support (and installation script).
- Windows App: add NLogs.
- Windows App: check for updates, possibility to get them.
- Console App: add interactive configuration with possibility
to save configuration into file so user might execute console
and then manually input configuration and have it saved for future use.
To run in interactive mode, simply launch console application without arguments.

## Changes

- Windows App: default directory is desktop.
- Console App: default directory is desktop.

# 2025.12.21

Initial version of FTPS Server UI for Windows on WPF platform.

# 2025.12.2

## New Features
- Library: Add compatibility with FluentFtp

# 2025.11.22

## New Features
- Library: First release to windows.
