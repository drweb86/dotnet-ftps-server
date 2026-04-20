# Ubuntu

There're several installation methods.

## Method 1. Installation via APT Repository (best)

This method lets you install and update FTPS Server using standard `apt` commands.

### One-time setup

Click copy and paste in the terminal.

```
curl -fsSL https://drweb86.github.io/dotnet-ftps-server/gpg-key.pub | sudo gpg --dearmor -o /usr/share/keyrings/ftps-server.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/ftps-server.gpg] https://drweb86.github.io/dotnet-ftps-server stable main" | sudo tee /etc/apt/sources.list.d/ftps-server.list > /dev/null
```

### Install

```
sudo apt update && sudo apt install ftps-server
```

### Update

```
sudo apt update && sudo apt upgrade ftps-server
```

### Uninstall

```
sudo apt remove ftps-server
sudo rm /etc/apt/sources.list.d/ftps-server.list /usr/share/keyrings/ftps-server.gpg
```

## Method 2. Installation via .deb Download

Download the `.deb` file for your architecture from the [latest release](https://github.com/drweb86/dotnet-ftps-server/releases/latest) and install:

```
sudo dpkg -i ftps-server_*_amd64.deb
sudo apt-get install -f
```

For ARM64:

```
sudo dpkg -i ftps-server_*_arm64.deb
sudo apt-get install -f
```

### Uninstall

```
sudo apt remove ftps-server
```

## Method 3. Installation via Bash script

Open terminal, paste

```
wget -O - https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/master/scripts/ubuntu-install.sh | bash
```

for preview version

```
wget -O - https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/master/scripts/ubuntu-install.sh | bash -s -- --latest
```

### Uninstallation (source install only)

```
wget -O - https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/master/scripts/ubuntu-uninstall.sh | bash
```

## Executables

After installation (APT or .deb), the following commands are available:

- **`ftps-server-ui`** — graphical user interface
- **`ftps-server`** — console tool
