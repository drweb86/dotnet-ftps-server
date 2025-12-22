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
echo

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
sudo /root/.dotnet/dotnet publish /p:Version=${version} /p:AssemblyVersion=${version} -c Release --property:PublishDir=${binariesInstallationDirectory} --use-current-runtime --self-contained

echo
echo Prepare PNG icon for Gnome, ico files are not handled
echo
sudo cp "${sourceCodeInstallationDirectory}/help/Assets/Icon 120x120.png" "${binariesInstallationDirectory}/Icon 120x120.png"

echo
echo Prepare shortcut
echo

temporaryShortcut=/tmp/FTPS.desktop
sudo rm -f ${temporaryShortcut}
cat > ${temporaryShortcut} << EOL
[Desktop Entry]
Encoding=UTF-8
Version=${version}
Name=FTPS
GenericName=FTPS Server for file sharing
Categories=FTPS
Comment=FTPS server for file sharing
Type=Application
Terminal=false
Exec=${binariesInstallationDirectory}/ftps-server
Icon=${binariesInstallationDirectory}/Icon 120x120.png
StartupWMClass=ftps-server
EOL
sudo chmod -R 775 ${temporaryShortcut}

desktopDir=$(xdg-user-dir DESKTOP)
declare -a shortcutLocations=("/usr/share/applications" "${desktopDir}")

for shortcutLocation in "${shortcutLocations[@]}"
do

shortcutFile=${shortcutLocation}/FTPS.desktop

echo
echo Create shortcut in ${shortcutFile}
echo

sudo cp ${temporaryShortcut} "${shortcutFile}"
sudo chmod -R 775 "${shortcutFile}"
gio set "${shortcutFile}" metadata::trusted true

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
echo Shortcut on desktop and for quick search are provisioned.
echo Console tool: ${binariesInstallationDirectory}/ftps-server
echo
echo
sleep 2m