# Build the Android FTPS Server APK, start an emulator if none is running,
# install the general debug APK, and open it.
# Usage (from the repo root):
#   powershell -File run-android-emulator.ps1
#   powershell -File run-android-emulator.ps1 -Release
#   powershell -File run-android-emulator.ps1 -Avd Medium_Phone_API_37.0
#   powershell -File run-android-emulator.ps1 -SkipBuild
# The general debug package id is com.siarheikuchuk.ftpsserver.debug.

param(
    [switch]$Release,
    [switch]$SkipBuild,
    [string]$Avd = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$PackageId = if ($Release) { "com.siarheikuchuk.ftpsserver" } else { "com.siarheikuchuk.ftpsserver.debug" }
$Activity = "com.siarheikuchuk.ftpsserver.MainActivity"

function Find-AndroidSdk {
    $candidates = @(
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        "${env:LOCALAPPDATA}\Android\Sdk",
        "${env:LOCALAPPDATA}\FtpsServerAndroidSdk",
        "${env:LOCALAPPDATA}\BUtilAndroidSdk"
    )
    foreach ($c in $candidates) {
        if ([string]::IsNullOrWhiteSpace($c)) { continue }
        if (Test-Path (Join-Path $c "platforms")) { return $c }
    }
    throw "Android SDK not found. Set ANDROID_HOME to your SDK directory."
}

function Find-LocalJdk {
    if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME "bin\java.exe"))) {
        return $env:JAVA_HOME
    }
    $roots = @(
        "${env:LOCALAPPDATA}\Java",
        "C:\Program Files\Android\openjdk",
        "C:\Program Files\Microsoft",
        "C:\Program Files\Eclipse Adoptium"
    )
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        $hit = Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path (Join-Path $_.FullName "bin\java.exe") } |
            Sort-Object Name -Descending |
            Select-Object -First 1
        if ($hit) { return $hit.FullName }
    }
    return $null
}

function Get-OnlineSerials([string]$Adb) {
    $serials = @()
    foreach ($line in (& $Adb devices)) {
        if ($line -match '^(?<id>\S+)\s+device$') {
            $serials += $Matches.id
        }
    }
    return $serials
}

function Get-AvdName([string]$Emulator, [string]$Requested) {
    $avds = @(& $Emulator -list-avds | Where-Object {
        $_ -and ($_ -notmatch '^(INFO|ERROR|WARNING)\b')
    })
    if ($avds.Count -eq 0) {
        throw "No Android Virtual Device found. Create one in Android Studio Device Manager, then run this script again."
    }
    if ($Requested) {
        $match = $avds | Where-Object { $_ -eq $Requested } | Select-Object -First 1
        if (-not $match) {
            throw "AVD '$Requested' was not found. Available: $($avds -join ', ')"
        }
        return $match
    }
    $preferred = $avds | Where-Object { $_ -eq "Medium_Phone_API_37.0" } | Select-Object -First 1
    if ($preferred) { return $preferred }
    return $avds[0]
}

function Wait-ForBoot([string]$Adb, [string]$Serial) {
    Write-Host "Waiting for $Serial to finish booting ..."
    & $Adb -s $Serial wait-for-device | Out-Null
    $deadline = (Get-Date).AddMinutes(4)
    do {
        $boot = (& $Adb -s $Serial shell getprop sys.boot_completed 2>$null)
        if ($boot) { $boot = $boot.Trim() }
        if ($boot -eq "1") {
            & $Adb -s $Serial shell input keyevent 82 | Out-Null
            return
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)
    throw "Emulator $Serial did not finish booting within 4 minutes."
}

function Install-Apk([string]$Adb, [string]$Serial, [string]$Apk) {
    Write-Host "Installing $PackageId on $Serial ..."
    $last = ""
    for ($attempt = 1; $attempt -le 8; $attempt++) {
        $output = & $Adb -s $Serial install -r $Apk 2>&1 | Out-String
        $last = $output.Trim()
        if ($LASTEXITCODE -eq 0 -and $output -match "Success") { return }
        Write-Host "Install attempt $attempt did not succeed yet. $last"
        Start-Sleep -Seconds 3
    }
    throw "adb install failed for $PackageId. $last"
}

$jdk = Find-LocalJdk
if ($jdk) { $env:JAVA_HOME = $jdk }

$sdk = Find-AndroidSdk
$adb = Join-Path $sdk "platform-tools\adb.exe"
$emulator = Join-Path $sdk "emulator\emulator.exe"
if (-not (Test-Path $adb)) { throw "adb not found at $adb" }
if (-not (Test-Path $emulator)) { throw "emulator not found at $emulator" }

$serial = Get-OnlineSerials $adb | Where-Object { $_ -like "emulator-*" } | Select-Object -First 1
if (-not $serial) {
    $avdName = Get-AvdName $emulator $Avd
    Write-Host "Starting emulator $avdName ..."
    Start-Process -FilePath $emulator -ArgumentList @("-avd", $avdName, "-netdelay", "none", "-netspeed", "full") | Out-Null
} else {
    Write-Host "Emulator already running: $serial"
}

if (-not $SkipBuild) {
    $buildScript = Join-Path $ProjectRoot "build-android.ps1"
    if ($Release) {
        & $buildScript -Release
        $apk = Join-Path $ProjectRoot "Output\ftpsserver_android.apk"
    } else {
        & $buildScript
        $apk = Join-Path $ProjectRoot "Output\ftpsserver_android_debug.apk"
    }
    if (-not (Test-Path $apk)) { throw "APK was not produced: $apk" }
} else {
    $apk = if ($Release) {
        Join-Path $ProjectRoot "Output\ftpsserver_android.apk"
    } else {
        Join-Path $ProjectRoot "Output\ftpsserver_android_debug.apk"
    }
    if (-not (Test-Path $apk)) { throw "APK was not found: $apk" }
}

$serial = Get-OnlineSerials $adb | Where-Object { $_ -like "emulator-*" } | Select-Object -First 1
if (-not $serial) { throw "Emulator did not appear in adb devices." }
Wait-ForBoot $adb $serial
Install-Apk $adb $serial $apk

Write-Host "Opening $PackageId ..."
& $adb -s $serial shell am start -n "$PackageId/$Activity" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not start $PackageId" }
Write-Host "OK"
