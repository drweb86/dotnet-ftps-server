@echo off
echo Building FTPS Server Application...
echo.

dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo.
    echo To run the application:
    echo   cd bin\Release\net10.0-windows
    echo   FtpsServerWpf.exe
    echo.
    pause
) else (
    echo.
    echo Build failed!
    echo.
    pause
)
