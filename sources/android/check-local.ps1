# Native Android FTPS Server — local build and optional device install.
# Usage (from anywhere):
#   powershell -File sources/android/check-local.ps1
# Optional: -Install  to adb install/launch if a device/emulator is connected
#           -Release  to assemble the release APK
#           -Screenshots  English-only APK for store screenshots (id ...ftpsserver.screenshots)
#           -VersionName / -VersionCode  (used for release versioning)

param(
    [switch]$Install,
    [switch]$Release,
    [switch]$Screenshots,
    [string]$VersionName = "",
    [string]$VersionCode = ""
)

$ErrorActionPreference = "Stop"
$AndroidRoot = $PSScriptRoot
$ProjectRoot = (Resolve-Path (Join-Path $AndroidRoot "..\..")).Path

function Find-JavaHome {
    $candidates = @(
        $env:JAVA_HOME,
        "C:\Program Files\Android\openjdk\jdk-21.0.8",
        "C:\Program Files\Android\openjdk\jdk-17.0.14",
        "C:\Program Files\Microsoft\jdk-21.0.8-hotspot",
        "C:\Program Files\Eclipse Adoptium\jdk-21*"
    )
    foreach ($c in $candidates) {
        if ([string]::IsNullOrWhiteSpace($c)) { continue }
        $resolved = Get-Item $c -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $resolved) { continue }
        $java = Join-Path $resolved.FullName "bin\java.exe"
        if (Test-Path $java) { return $resolved.FullName }
    }
    throw "JDK 17+ not found. Install a JDK or set JAVA_HOME."
}

function Find-AndroidSdk {
    $candidates = @(
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        "${env:LOCALAPPDATA}\Android\Sdk",
        "${env:ProgramFiles(x86)}\Android\android-sdk",
        "$env:ProgramFiles\Android\android-sdk"
    )
    foreach ($c in $candidates) {
        if ([string]::IsNullOrWhiteSpace($c)) { continue }
        if (Test-Path (Join-Path $c "platforms")) { return $c }
    }
    throw "Android SDK not found. Set ANDROID_HOME to your SDK directory."
}

function New-WritableSdk($sdk) {
    $localSdk = Join-Path $env:LOCALAPPDATA "FtpsServerAndroidSdk"
    New-Item -ItemType Directory -Force -Path $localSdk | Out-Null
    foreach ($name in @("platforms", "build-tools", "platform-tools", "cmdline-tools")) {
        $src = Join-Path $sdk $name
        $dst = Join-Path $localSdk $name
        if (-not (Test-Path $src)) { continue }
        if (Test-Path $dst) { continue }
        cmd /c mklink /J "$dst" "$src" | Out-Null
        if ($LASTEXITCODE -ne 0 -and -not (Test-Path $dst)) {
            Write-Host "Could not link $name; using original SDK path."
            return $sdk
        }
    }
    $licenses = Join-Path $localSdk "licenses"
    New-Item -ItemType Directory -Force -Path $licenses | Out-Null
    Set-Content -Path (Join-Path $licenses "android-sdk-license") -Value "24333f8a63b6825ea9c5514f83c2829b004d1fee" -Encoding ASCII
    return $localSdk
}

$javaHome = Find-JavaHome
$sdk = New-WritableSdk (Find-AndroidSdk)
$env:JAVA_HOME = $javaHome
$env:ANDROID_HOME = $sdk
$env:ANDROID_SDK_ROOT = $sdk
$env:Path = "$javaHome\bin;$sdk\platform-tools;$env:Path"

Write-Host "JAVA_HOME    = $javaHome"
Write-Host "ANDROID_HOME = $sdk"

$sdkDirProp = ($sdk -replace '\\', '\\' -replace ':', '\:')
Set-Content -Path (Join-Path $AndroidRoot "local.properties") -Value "sdk.dir=$sdkDirProp" -Encoding ASCII

$iconSrc = Join-Path $ProjectRoot "sources\FtpsServerAvalonia\FtpsServerAvalonia.Android\Icon.png"
$iconDst = Join-Path $AndroidRoot "app\src\main\res\drawable\ic_launcher.png"
if (Test-Path $iconSrc) {
    Copy-Item $iconSrc $iconDst -Force
}

$py = Get-Command python -ErrorAction SilentlyContinue
if ($py) {
    Write-Host "Converting localization resx -> strings.xml"
    & python (Join-Path $AndroidRoot "tools\convert_resx.py")
}

$wrapperJar = Join-Path $AndroidRoot "gradle\wrapper\gradle-wrapper.jar"
if (-not (Test-Path $wrapperJar)) {
    Write-Host "Downloading Gradle wrapper jar..."
    $url = "https://github.com/gradle/gradle/raw/v8.11.1/gradle/wrapper/gradle-wrapper.jar"
    Invoke-WebRequest -Uri $url -OutFile $wrapperJar -UseBasicParsing
}

$task = if ($Release) { "assembleRelease" } else { "assembleDebug" }
Write-Host "Building $task ..."
Push-Location $AndroidRoot
try {
    $gradleArgs = @("--no-daemon", $task)
    if ($VersionName) { $gradleArgs += "-PappVersionName=$VersionName" }
    if ($VersionCode) { $gradleArgs += "-PappVersionCode=$VersionCode" }
    if ($Screenshots) { $gradleArgs += "-Pscreenshots=true" }
    & .\gradlew.bat @gradleArgs
    if ($LASTEXITCODE -ne 0) { throw "Gradle $task failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

$apk = if ($Release) {
    $releaseDir = Join-Path $AndroidRoot "app\build\outputs\apk\release"
    @("app-release.apk", "app-release-unsigned.apk") |
        ForEach-Object { Join-Path $releaseDir $_ } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
} else {
    Join-Path $AndroidRoot "app\build\outputs\apk\debug\app-debug.apk"
}
if (-not $apk -or -not (Test-Path $apk)) {
    $apk = Get-ChildItem (Join-Path $AndroidRoot "app\build\outputs\apk") -Recurse -Filter "*.apk" | Select-Object -First 1 -ExpandProperty FullName
}
Write-Host "APK: $apk"
if ($apk) { Write-Host ("Size: {0:N2} MB" -f ((Get-Item $apk).Length / 1MB)) }

if ($Install) {
    $adb = Join-Path $sdk "platform-tools\adb.exe"
    if (-not (Test-Path $adb)) { throw "adb not found at $adb" }
    $devices = & $adb devices | Select-String "`tdevice$"
    if (-not $devices) {
        Write-Host "No Android device/emulator with status 'device'. Start an emulator or plug in a phone, then re-run with -Install."
        exit 0
    }
    Write-Host "Installing on device..."
    & $adb install -r $apk
    if ($LASTEXITCODE -ne 0) { throw "adb install failed" }
    $pkg = if ($Screenshots) {
        "com.siarheikuchuk.ftpsserver.screenshots"
    } elseif ($Release) {
        "com.siarheikuchuk.ftpsserver"
    } else {
        "com.siarheikuchuk.ftpsserver.debug"
    }
    & $adb shell am start -n "$pkg/com.siarheikuchuk.ftpsserver.MainActivity"
    Write-Host "Launched $pkg"
}

Write-Host "OK"
