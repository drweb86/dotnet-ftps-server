Write-Output "Building for Android......................................"

$version = Get-Content ".\CHANGELOG.md" -First 1
$version = $version.Substring(2)

Clear-Host
Write-Output "Version is $version"
$ErrorActionPreference = "Stop"

cd sources

# "--runtime=$($platform.CoreRuntimeWindows)" `
Write-Output "Publish..."

$maxRetries = 3
$retryCount = 0
$success = $false

while (-not $success -and $retryCount -lt $maxRetries) {
	$retryCount++
	Write-Output "Attempt $retryCount of $maxRetries..."

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

	if ($LastExitCode -eq 0) {
		$success = $true
	} elseif ($retryCount -lt $maxRetries) {
		Write-Output "Build failed. Retrying in 2 seconds..."
		Start-Sleep -Seconds 2
	}
}

if (-not $success) {
	Write-Error "Build failed after $maxRetries attempts."
	cd ..
	Exit 1
}

cd ..

Write-Output "Copying APK files..."

Move-Item -Path "sources\FtpsServerAvalonia\FtpsServerAvalonia.Android\bin\Release\net10.0-android\com.SiarheiKuchuk.FtpsServer-Signed.apk" -Destination "Output\com.SiarheiKuchuk.FtpsServer-$version.apk" -Force

Write-Output "Building for Android is completed......................."

