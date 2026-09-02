Write-Output "Building for Android......................................"

$version = Get-Content ".\CHANGELOG.md" -First 1
$version = $version.Substring(2)
$versionCode = $version -replace '\.', ''

Write-Output "Version is $version"
$ErrorActionPreference = "Stop"

$script = Join-Path $PSScriptRoot "sources\android\check-local.ps1"
& $script -Release -VersionName $version -VersionCode $versionCode
if ($LASTEXITCODE -ne 0) { Exit $LASTEXITCODE }

$releaseDir = Join-Path $PSScriptRoot "sources\android\app\build\outputs\apk\general\release"
$apk = @("app-general-release.apk", "app-release-unsigned.apk") |
    ForEach-Object { Join-Path $releaseDir $_ } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1
if (-not $apk) {
    Write-Error "Release APK not found under $releaseDir"
    Exit 1
}

New-Item -ItemType Directory -Force -Path (Join-Path $PSScriptRoot "Output") | Out-Null
$dest = Join-Path $PSScriptRoot "Output\ftpsserver_${version}_android.apk"
Copy-Item $apk $dest -Force
Write-Output "Copied $apk -> $dest"

Write-Output "Building for Android is completed......................."
