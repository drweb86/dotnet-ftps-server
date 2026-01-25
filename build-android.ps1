Write-Output "Building for Android......................................"

$version = Get-Content ".\CHANGELOG.md" -First 1
$version = $version.Substring(2)

Clear-Host
Write-Output "Version is $version"
$ErrorActionPreference = "Stop"

cd sources

# "--runtime=$($platform.CoreRuntimeWindows)" `
Write-Output "Publish..."
& dotnet publish FtpsServer.Android.slnx "/p:InformationalVersion=$version" `
	"/p:VersionPrefix=$version" `
	"/p:Version=$version" `
	"/p:AssemblyVersion=$version" `
	/p:Configuration=Release `
	"/p:PublishDir=../../Output/publish/Android" `
	/p:PublishReadyToRun=false `
	/p:RunAnalyzersDuringBuild=False `
	--self-contained true `
	--property WarningLevel=0

if ($LastExitCode -ne 0)
{
	Write-Error "Fail." 
	Exit 1
}

cd ..

Write-Output "Copying APK files..."
Copy-Item -Path "sources\FtpsServerAvalonia\FtpsServerAvalonia.Android\bin\Release\net10.0-android\com.SiarheiKuchuk.FtpsServer-Signed.apk" -Destination "Output" -Force
Rename-Item -Path "Output\com.SiarheiKuchuk.FtpsServer-Signed.apk" -NewName "com.SiarheiKuchuk.FtpsServer-$version.apk"

Write-Output "Building for Android is completed......................."

