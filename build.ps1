$version = Get-Content ".\CHANGELOG.md" -First 1
$version = $version.Substring(2)

Clear-Host
Write-Output "Version is $version"
$ErrorActionPreference = "Stop"

Write-Output "Clear Output folder..."
$outputPath=".\Output"; $retries=3
while ($retries -gt 0 -and (Test-Path $outputPath)) {
    Remove-Item $outputPath -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path $outputPath) { Start-Sleep 2; $retries-- }
}

class BuildInfo {
  [string]$CoreRuntimeWindows
  [string]$CoreRuntimeFolderPrefix

    BuildInfo(
	[string]$CoreRuntimeWindows,
	[string]$CoreRuntimeFolderPrefix) {
        $this.CoreRuntimeWindows = $CoreRuntimeWindows
	$this.CoreRuntimeFolderPrefix = $CoreRuntimeFolderPrefix
    }
}

$platforms = New-Object System.Collections.ArrayList
[void]$platforms.Add([BuildInfo]::new("win-x64", "x64"))
[void]$platforms.Add([BuildInfo]::new("win-arm64", "arm64"))
cd sources
ForEach ($platform in $platforms)
{
	Write-Output "Platform: $($platform.CoreRuntimeWindows)"

	Write-Output "Publish..."
	& dotnet publish FtpsServer.slnx "/p:InformationalVersion=$version" `
		"/p:VersionPrefix=$version" `
		"/p:Version=$version" `
		"/p:AssemblyVersion=$version" `
		"--runtime=$($platform.CoreRuntimeWindows)" `
		/p:Configuration=Release `
		"/p:PublishDir=../../Output/publish/$($platform.CoreRuntimeFolderPrefix)" `
		/p:PublishReadyToRun=false `
		/p:RunAnalyzersDuringBuild=False `
		--self-contained true `
		--property WarningLevel=0
	if ($LastExitCode -ne 0)
	{
		Write-Error "Fail." 
		Exit 1
	}
}
cd ..

Write-Output "Prepare to pack binaries"
ls
Copy-Item ".\README.md" -Destination ".\Output\publish"
if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

Write-Output "Setup..."
& "C:\Program Files (x86)\NSIS\Bin\makensis.exe" "/DPRODUCT_VERSION=$version" "./scripts/setup.nsi"
if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

Write-Output "Pack binaries"
& "c:\Program Files\7-Zip\7z.exe" a -y ".\Output\FtpsServer_v$($version)_win-binaries.7z" ".\Output\publish\*" -mx9 -t7z -m0=lzma2 -ms=on -sccUTF-8 -ssw
if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

Write-Output "Clear binaries"
Remove-Item ".\Output\publish" -Confirm:$false -Recurse:$true
if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

& ./build-android.ps1

Write-Output "The following artefacts are produced. Release them"
Get-ChildItem ".\Output"

# Write-Output "The following artefacts are produced. Release to win-get."
# Get-ChildItem ".\Output" *.exe | Get-FileHash

Write-Output "A. Release files were put into win-get repo fork. Release it"
# Write-Output "B. Commit changed ubuntu-install to main repository."