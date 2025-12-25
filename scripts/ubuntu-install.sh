#!/bin/bash

# Fail on first error.
set -e

version=2025.12.24

sourceCodeInstallationDirectory=/usr/local/src/dotnet-ftps-server
binariesInstallationDirectory=/usr/local/dotnet-ftps-server

if [ "$EUID" -eq 0 ]
  then echo "Please do not run this script with sudo, root permissions"
  exit
fi

echo
echo Install .Net 10
echo
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 10.0
sudo apt install dbus-x11

echo
echo Cleaning installation directories
echo
sudo rm -rf ${sourceCodeInstallationDirectory}
sudo rm -rf ${binariesInstallationDirectory}

echo
echo Get source code
echo
sudo git clone https://github.com/drweb86/dotnet-ftps-server.git ${sourceCodeInstallationDirectory}
cd ${sourceCodeInstallationDirectory}

echo
echo Update to tag
echo
sudo git checkout tags/${version}

echo
echo Building
echo
cd ./sources
sudo /root/.dotnet/dotnet publish FtpsServer.Ubuntu.slnx /p:Version=${version} /p:AssemblyVersion=${version} -c Release --property:PublishDir=${binariesInstallationDirectory} --use-current-runtime --self-contained

echo
echo Prepare PNG icon for Gnome, ico files are not handled
echo
sudo cp "${sourceCodeInstallationDirectory}/sources/FtpsServerWindows/FtpsApp.png" "${binariesInstallationDirectory}/Icon.png"

echo
echo Prepare shortcut
echo

temporaryShortcut=/tmp/FTPS.desktop
sudo rm -f ${temporaryShortcut}
cat > ${temporaryShortcut} << EOL
[Desktop Entry]
Version=${version}
Name=FTPS
GenericName=FTPS Server for file sharing
Categories=Network;System;Utility;
Comment=FTPS server for file sharing
Keywords=ftp;sftp;file;transfer;server
Type=Application
Terminal=true
Path=${binariesInstallationDirectory}
Exec=${binariesInstallationDirectory}/ftps-server
Icon=${binariesInstallationDirectory}/Icon.png
EOL

echo
echo Prepare shortcut for All Users
echo

declare -a shortcutLocations=("/usr/share/applications")

for shortcutLocation in "${shortcutLocations[@]}"
do

shortcutFile=${shortcutLocation}/FTPS.desktop

echo
echo Create shortcut in ${shortcutFile}
echo

sudo cp ${temporaryShortcut} "${shortcutFile}"
sudo chmod a+x "${shortcutFile}"
sudo dbus-launch gio set "${shortcutFile}" metadata::trusted true

done

echo
echo
echo Everything is completed 
echo
echo
echo Application was installed too:
echo
echo Binaries: ${binariesInstallationDirectory}
echo Sources: ${sourceCodeInstallationDirectory}
echo
echo Shortcut for quick search is provisioned.
echo Console tool: ${binariesInstallationDirectory}/ftps-server
echo
echo
sleep 2m
