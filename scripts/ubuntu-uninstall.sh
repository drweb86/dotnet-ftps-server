#!/bin/bash

echo
echo Removing sources installation folder
echo
sudo rm -rf /usr/local/src/dotnet-ftps-server

echo
echo Removing binaries installation folder
echo
sudo rm -rf /usr/local/dotnet-ftps-server

echo
echo Removing configuration files
echo

desktopDir=$(xdg-user-dir DESKTOP)

echo
echo Removing shortcuts
echo
sudo rm /usr/share/applications/FTPS.desktop
sudo rm "${desktopDir}/FTPS.desktop"

echo
echo Application was uninstalled
echo
echo
echo
sleep 2m