# Native Android FTPS Server — local build and optional device install.
# Usage (from anywhere):
#   powershell -File sources/android/check-local.ps1
# Default debug copies three APKs into the repo Output folder:
#   Output/ftpsserver_android_screenshots_debug.apk
#   Output/ftpsserver_android_debug.apk
#   Output/ftpsserver_android_china_debug.apk
# Optional: -Install  adb install all three if a device/emulator is connected
#           -Release  assemble one release APK instead
#           -ChinaPipl  with -Release: China PIPL-policy flavor
#           -VersionName / -VersionCode  (used for release versioning)

param(
    [switch]$Install,
    [switch]$Release,
    [switch]$ChinaPipl,
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

function Invoke-Gradle([string[]]$Tasks, [string[]]$ExtraArgs) {
    $gradleArgs = @("--no-daemon") + $Tasks
    if ($VersionName) { $gradleArgs += "-PappVersionName=$VersionName" }
    if ($VersionCode) { $gradleArgs += "-PappVersionCode=$VersionCode" }
    if ($ExtraArgs) { $gradleArgs += $ExtraArgs }
    Write-Host "Building $($Tasks -join ', ') ..."
    Push-Location $AndroidRoot
    try {
        & .\gradlew.bat @gradleArgs
        if ($LASTEXITCODE -ne 0) { throw "Gradle $($Tasks -join ' ') failed with exit code $LASTEXITCODE" }
    } finally {
        Pop-Location
    }
}

function Copy-ToOutput($src, $fileName) {
    if (-not $src -or -not (Test-Path $src)) {
        throw "APK not found: $src"
    }
    $outDir = Join-Path $ProjectRoot "Output"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $dest = Join-Path $outDir $fileName
    Copy-Item $src $dest -Force
    Write-Host ("{0}: {1}" -f $fileName, $dest)
    Write-Host ("  Size: {0:N2} MB" -f ((Get-Item $dest).Length / 1MB))
    return $dest
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

$apkOutputs = Join-Path $AndroidRoot "app\build\outputs\apk"
$installList = @()

if ($Release) {
    $flavorTask = if ($ChinaPipl) { "ChinaPiplPolicy" } else { "General" }
    Invoke-Gradle @("assemble${flavorTask}Release") @()
    $flavorDir = if ($ChinaPipl) { "chinaPiplPolicy" } else { "general" }
    $apkName = if ($ChinaPipl) { "app-chinaPiplPolicy-release.apk" } else { "app-general-release.apk" }
    $releaseDir = Join-Path $apkOutputs "$flavorDir\release"
    $apk = @($apkName, "app-release-unsigned.apk") |
        ForEach-Object { Join-Path $releaseDir $_ } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    $apk = Copy-ToOutput $apk $(if ($ChinaPipl) { "ftpsserver_android_china.apk" } else { "ftpsserver_android.apk" })
    $pkg = "com.siarheikuchuk.ftpsserver"
    $installList += @{ Path = $apk; Package = $pkg }
} else {
    # Screenshots uses -Pscreenshots=true, which cannot be mixed with the other
    # debug variants in one Gradle run (it would rewrite applicationId for all).
    Invoke-Gradle @("assembleGeneralDebug") @("-Pscreenshots=true")
    $screenshotsSrc = Join-Path $apkOutputs "general\debug\app-general-debug.apk"
    $screenshotsApk = Copy-ToOutput $screenshotsSrc "ftpsserver_android_screenshots_debug.apk"

    Invoke-Gradle @("assembleGeneralDebug", "assembleChinaPiplPolicyDebug") @()
    $generalApk = Copy-ToOutput (Join-Path $apkOutputs "general\debug\app-general-debug.apk") "ftpsserver_android_debug.apk"
    $chinaApk = Copy-ToOutput (Join-Path $apkOutputs "chinaPiplPolicy\debug\app-chinaPiplPolicy-debug.apk") "ftpsserver_android_china_debug.apk"

    $installList += @{ Path = $screenshotsApk; Package = "com.siarheikuchuk.ftpsserver.screenshots" }
    $installList += @{ Path = $generalApk; Package = "com.siarheikuchuk.ftpsserver.debug" }
    $installList += @{ Path = $chinaApk; Package = "com.siarheikuchuk.ftpsserver.chinapipl.debug" }
}

if ($Install) {
    $adb = Join-Path $sdk "platform-tools\adb.exe"
    if (-not (Test-Path $adb)) { throw "adb not found at $adb" }
    $devices = & $adb devices | Select-String "`tdevice$"
    if (-not $devices) {
        Write-Host "No Android device/emulator with status 'device'. Start an emulator or plug in a phone, then re-run with -Install."
        exit 0
    }
    foreach ($item in $installList) {
        Write-Host "Installing $($item.Package) ..."
        & $adb install -r $item.Path
        if ($LASTEXITCODE -ne 0) { throw "adb install failed for $($item.Package)" }
    }
    Write-Host "Installed $($installList.Count) APK(s). Open them from the launcher (distinct names)."
}

Write-Host "OK"
