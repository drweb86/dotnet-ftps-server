#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$REPO_ROOT/Output"

cd "$SCRIPT_DIR"

version=$(head -1 "$REPO_ROOT/CHANGELOG.md" | sed 's/^\xEF\xBB\xBF//' | sed 's/^# //')
echo "Building FTPS Server v$version deb packages"
echo ""

rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

declare -A ARCH_MAP
ARCH_MAP["linux-x64"]="amd64"
ARCH_MAP["linux-arm64"]="arm64"

for rid in "${!ARCH_MAP[@]}"; do
    deb_arch="${ARCH_MAP[$rid]}"
    publish_dir="$OUTPUT_DIR/staging/$deb_arch/publish"
    pkg_root="$OUTPUT_DIR/staging/$deb_arch/pkg"
    deb_file="$OUTPUT_DIR/ftps-server_${version}_${deb_arch}.deb"

    echo "========================================="
    echo "  Building $rid ($deb_arch)"
    echo "========================================="
    echo ""

    rm -rf "$OUTPUT_DIR/staging/$deb_arch"

    echo "Publishing..."
    dotnet publish FtpsServer.Ubuntu.slnx \
        "/p:InformationalVersion=$version" \
        "/p:VersionPrefix=$version" \
        "/p:Version=$version" \
        "/p:AssemblyVersion=$version" \
        "--runtime=$rid" \
        -c Release \
        "/p:PublishDir=$publish_dir" \
        /p:PublishReadyToRun=false \
        /p:RunAnalyzersDuringBuild=False \
        --self-contained true \
        --property WarningLevel=0

    echo "Creating package structure..."
    mkdir -p "$pkg_root/DEBIAN"
    mkdir -p "$pkg_root/usr/lib/ftps-server"
    mkdir -p "$pkg_root/usr/bin"
    mkdir -p "$pkg_root/usr/share/applications"
    mkdir -p "$pkg_root/usr/share/pixmaps"

    cp -a "$publish_dir/"* "$pkg_root/usr/lib/ftps-server/"

    ln -sf ../lib/ftps-server/FtpsServerAvalonia.Desktop "$pkg_root/usr/bin/ftps-server-ui"
    ln -sf ../lib/ftps-server/ftps-server "$pkg_root/usr/bin/ftps-server"

    cp "$SCRIPT_DIR/FtpsServerAvalonia/FtpsServerAvalonia/Assets/FtpsApp.png" "$pkg_root/usr/share/pixmaps/ftps-server.png"

    cat > "$pkg_root/usr/share/applications/ftps-server.desktop" << 'DESKTOP'
[Desktop Entry]
Version=1.0
Name=FTPS Server
GenericName=FTPS File Server
Comment=Share files via FTPS between devices over network
Categories=Utility;Network;FileTransfer;
Type=Application
Terminal=false
Exec=ftps-server-ui
Icon=ftps-server
StartupWMClass=FtpsServerAvalonia.Desktop
DESKTOP

    installed_size=$(du -sk "$pkg_root" | cut -f1)

    cat > "$pkg_root/DEBIAN/control" << CONTROL
Package: ftps-server
Version: $version
Section: net
Priority: optional
Architecture: $deb_arch
Installed-Size: $installed_size
Depends: libc6, libgcc-s1, libstdc++6, libx11-6, libfontconfig1
Maintainer: Siarhei Kuchuk <https://github.com/drweb86>
Homepage: https://github.com/drweb86/dotnet-ftps-server
Description: FTPS (FTP over TLS) server for sharing files between devices
 FTPS Server allows sharing files via FTPS between devices over network.
 .
 Features: user permissions, per-user root folders, path security,
 UTF-8 support, graphical and console interfaces.
CONTROL

    cat > "$pkg_root/DEBIAN/postinst" << 'POSTINST'
#!/bin/bash
set -e
chmod +x /usr/lib/ftps-server/FtpsServerAvalonia.Desktop
chmod +x /usr/lib/ftps-server/ftps-server
if command -v update-desktop-database > /dev/null 2>&1; then
    update-desktop-database -q /usr/share/applications || true
fi
if [ -n "$SUDO_USER" ]; then
    DESKTOP_DIR=$(su - "$SUDO_USER" -c 'xdg-user-dir DESKTOP' 2>/dev/null) || true
    if [ -n "$DESKTOP_DIR" ] && [ -d "$DESKTOP_DIR" ]; then
        cp /usr/share/applications/ftps-server.desktop "$DESKTOP_DIR/FtpsServer.desktop"
        chown "$SUDO_USER":"$SUDO_USER" "$DESKTOP_DIR/FtpsServer.desktop"
        chmod 755 "$DESKTOP_DIR/FtpsServer.desktop"
        su - "$SUDO_USER" -c "gio set '$DESKTOP_DIR/FtpsServer.desktop' metadata::trusted true" 2>/dev/null || true
    fi
fi
POSTINST
    chmod 755 "$pkg_root/DEBIAN/postinst"

    cat > "$pkg_root/DEBIAN/postrm" << 'POSTRM'
#!/bin/bash
set -e
if command -v update-desktop-database > /dev/null 2>&1; then
    update-desktop-database -q /usr/share/applications || true
fi
if [ -n "$SUDO_USER" ]; then
    DESKTOP_DIR=$(su - "$SUDO_USER" -c 'xdg-user-dir DESKTOP' 2>/dev/null) || true
    if [ -n "$DESKTOP_DIR" ]; then
        rm -f "$DESKTOP_DIR/FtpsServer.desktop"
    fi
fi
POSTRM
    chmod 755 "$pkg_root/DEBIAN/postrm"

    echo "Setting permissions..."
    find "$pkg_root/usr" -type d -exec chmod 755 {} \;
    find "$pkg_root/usr/lib/ftps-server" -type f -exec chmod 644 {} \;
    chmod 755 "$pkg_root/usr/lib/ftps-server/FtpsServerAvalonia.Desktop"
    chmod 755 "$pkg_root/usr/lib/ftps-server/ftps-server"
    find "$pkg_root/usr/lib/ftps-server" \( -name "*.so" -o -name "*.so.*" \) -exec chmod 755 {} \;
    chmod 644 "$pkg_root/usr/share/applications/ftps-server.desktop"
    chmod 644 "$pkg_root/usr/share/pixmaps/ftps-server.png"

    echo "Building .deb..."
    dpkg-deb --build --root-owner-group "$pkg_root" "$deb_file"

    echo "Created: $deb_file"
    echo ""
done

rm -rf "$OUTPUT_DIR/staging"

echo "========================================="
echo "  Build complete"
echo "========================================="
ls -lh "$OUTPUT_DIR"/*.deb
