$version = Get-Content ".\CHANGELOG.md" -First 1
$version = $version.Substring(2)

Clear-Host
Write-Output "Version is $version"
$ErrorActionPreference = "Stop"

Write-Output "Clear Output folder..."
if (Test-Path ".\Output")
{
	Remove-Item ".\Output" -Confirm:$false -Recurse:$true
	if ($LastExitCode -ne 0)
	{
		Write-Error "Fail." 
		Exit 1
	}
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
# [void]$platforms.Add([BuildInfo]::new("win-arm64", "arm64"))

ForEach ($platform in $platforms)
{
	Write-Output "Platform: $($platform.CoreRuntimeWindows)"

	Write-Output "Publish..."
	& dotnet publish ./sources/FtpsServerMaui/FtpsServerMaui.csproj `
        "/p:InformationalVersion=$version" `
		"/p:VersionPrefix=$version" `
		"/p:Version=$version" `
		"/p:AssemblyVersion=$version" `
        "-p:RuntimeIdentifierOverride=$($platform.CoreRuntimeWindows)" `
		"--runtime=$($platform.CoreRuntimeWindows)" `
		/p:Configuration=Release `
		"/p:PublishDir=./Output/publish/$($platform.CoreRuntimeFolderPrefix)" `
		--property WarningLevel=0 `
   		/p:PublishReadyToRun=false `
		/p:RunAnalyzersDuringBuild=False `
		--self-contained true `
        -f net10.0-windows10.0.19041.0
	if ($LastExitCode -ne 0)
	{
		Write-Error "Fail." 
		Exit 1
	}
}

# & dotnet publish ./FtpsServerMaui.csproj `
#     -f net10.0-windows10.0.19041.0  `
#     -c Release  `
#     -p:RuntimeIdentifierOverride=win10-x64  `
#     "/p:PublishDir=./Output/win10-x64"