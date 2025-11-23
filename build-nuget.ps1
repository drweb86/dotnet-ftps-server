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

Write-Output "Getting nuget..."

$nugetApp="$env:TEMP\nuget-packager\nuget.exe"
if (-not (Test-Path $nugetApp))
{
	md "$env:TEMP\nuget-packager"

	Write-Output "Downloading nuget..."
	Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile "$nugetApp"
	Unblock-File $nugetApp
}

Write-Output "Build Nuget"
& dotnet `
	pack sources\FtpsServerLibrary\FtpsServerLibrary.csproj `
	-c Release `
	-o output\nuget-package `
	/p:InformationalVersion=$version `
	/p:VersionPrefix=$version `
	/p:Version=$version `
	/p:AssemblyVersion=$version `
	/p:Configuration=Release

if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

Write-Output "Publish Nuget"
$nugetKeyFile="..\nuget-api-key.key"
if (-not (Test-Path $nugetKeyFile))
{
	Write-Error "File $nugetKeyFile is missing!"
	Exit 1
}

$nugetKey = Get-Content $nugetKeyFile -First 1
& dotnet nuget push output\nuget-package\Siarhei_Kuchuk.FtpsServerLibrary.$version.nupkg `
    --api-key $nugetKey `
    --source https://api.nuget.org/v3/index.json

if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

Write-Output "Success."