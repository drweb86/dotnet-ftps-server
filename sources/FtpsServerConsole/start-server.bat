@echo off
REM FTPS Server Startup Script for Windows
REM This script provides easy startup options for the FTPS server

echo ========================================
echo    FTPS Server - Windows Launcher
echo ========================================
echo.

:MENU
echo Choose an option:
echo.
echo 1. Start with default configuration (appsettings.json)
echo 2. Start with custom settings
echo 3. Start with command-line user setup
echo 4. Generate self-signed certificate
echo 5. Build project
echo 6. View help
echo 7. Exit
echo.
set /p choice="Enter your choice (1-7): "

if "%choice%"=="1" goto START_DEFAULT
if "%choice%"=="2" goto START_CUSTOM
if "%choice%"=="3" goto START_CMDLINE
if "%choice%"=="4" goto GEN_CERT
if "%choice%"=="5" goto BUILD
if "%choice%"=="6" goto HELP
if "%choice%"=="7" goto END

echo Invalid choice. Please try again.
echo.
goto MENU

:START_DEFAULT
echo.
echo Starting server with appsettings.json...
echo.
dotnet run
goto END

:START_CUSTOM
echo.
set /p configfile="Enter path to config file: "
echo Starting server with %configfile%...
echo.
dotnet run -- --config "%configfile%"
goto END

:START_CMDLINE
echo.
echo Example: --ip 0.0.0.0 --port 2121 --user admin:pass:/root:RWDCXN
set /p args="Enter command-line arguments: "
echo Starting server...
echo.
dotnet run -- %args%
goto END

:GEN_CERT
echo.
echo Generating self-signed certificate...
echo This requires Administrator privileges.
echo.
echo Running PowerShell command...
powershell -Command "New-SelfSignedCertificate -DnsName 'localhost' -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(10)"
echo.
echo Certificate generated successfully!
echo.
pause
goto MENU

:BUILD
echo.
echo Building project...
echo.
dotnet restore
dotnet build -c Release
echo.
echo Build complete!
echo.
pause
goto MENU

:HELP
echo.
echo Command-line options:
echo   --config ^<path^>              Path to JSON configuration file
echo   --ip ^<address^>               IP address to bind
echo   --port ^<number^>              Port number
echo   --cert ^<path^>                Certificate file path (.pfx)
echo   --certpass ^<password^>        Certificate password
echo   --user ^<name:pass:folder:perms^>  Add user
echo.
echo User permission format: username:password:folder:RWDCXN
echo   R = Read, W = Write
echo.
echo Examples:
echo   --user admin:pass123:/home/admin:RWDCXN
echo   --user readonly:view:/public:R
echo.
pause
goto MENU

:END
echo.
echo Goodbye!
pause
