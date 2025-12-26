#!/bin/bash

# Fail on first error.
set -e

version=APP_VERSION_STRING

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

echo
echo Prepare shortcut for Console
echo

temporaryShortcutConsole=/tmp/FTPS-console.desktop
sudo rm -f ${temporaryShortcutConsole}
cat > ${temporaryShortcutConsole} << EOL
[Desktop Entry]
Version=${version}
Name=FTPS Console
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
shortcutFileConsole=/usr/share/applications/FTPS-console.desktop
sudo cp ${temporaryShortcutConsole} "${shortcutFileConsole}"
sudo chmod a+x "${shortcutFileConsole}"
sudo dbus-launch gio set "${shortcutFileConsole}" metadata::trusted true

echo
echo Prepare shortcut for UI
echo

temporaryShortcutUI=/tmp/FTPS.desktop
sudo rm -f ${temporaryShortcutUI}
cat > ${temporaryShortcutUI} << EOL
[Desktop Entry]
Version=${version}
Name=FTPS UI
GenericName=FTPS Server for file sharing
Categories=Network;System;Utility;
Comment=FTPS server for file sharing
Keywords=ftp;sftp;file;transfer;server
Type=Application
Terminal=false
StartupWMClass=FtpsServerAvalonia.Desktop
Path=${binariesInstallationDirectory}
Exec=${binariesInstallationDirectory}/FtpsServerAvalonia.Desktop
Icon=${binariesInstallationDirectory}/Icon.png
EOL
shortcutFileUI=/usr/share/applications/FTPS.desktop
sudo cp ${temporaryShortcutUI} "${shortcutFileUI}"
sudo chmod a+x "${shortcutFileUI}"
sudo dbus-launch gio set "${shortcutFileUI}" metadata::trusted true

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
echo Shortcuts for quick search is provisioned:
echo     search menu for FTPS UI or FTPS Console.
echo
echo UI tool: ${binariesInstallationDirectory}/FtpsServerAvalonia.Desktop
echo Console tool: ${binariesInstallationDirectory}/ftps-server
echo
echo
sleep 2m