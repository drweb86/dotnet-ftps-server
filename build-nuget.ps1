$version = Get-Content ".\CHANGELOG.md" -First 1
$version = $version.Substring(2)

Clear-Host
Write-Output "Version is $version"
$ErrorActionPreference = "Stop"

Write-Output "Deleting everything untracked/non commited."
pause

git clean -ffdx
if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Set-Location sources
	Exit 1
}

Write-Output "Clear bin/obj folders..."
Get-ChildItem .\ -include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.FullName -Force -Recurse }
if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

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
[void]$platforms.Add([BuildInfo]::new("win-arm64", "arm64"))

ForEach ($platform in $platforms)
{
	Write-Output "Platform: $($platform.CoreRuntimeWindows)"

	Write-Output "Publish..."
	& nuget pack sources\FtpsServerLibrary\FtpsServerLibrary.csproj `
		-NonInteractive `
		-OutputDirectory=output\nuget-package `
		"-Version=$version" `
		"/p:InformationalVersion=$version" `
		"/p:VersionPrefix=$version" `
		"/p:Version=$version" `
		"/p:AssemblyVersion=$version" `
		/p:Configuration=Release
	if ($LastExitCode -ne 0)
	{
		Write-Error "Fail." 
		Exit 1
	}
}

