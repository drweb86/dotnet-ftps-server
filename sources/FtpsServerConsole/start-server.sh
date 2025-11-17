#!/bin/bash
# FTPS Server Startup Script for Linux/macOS

echo "========================================"
echo "   FTPS Server - Linux/macOS Launcher"
echo "========================================"
echo ""

show_menu() {
    echo "Choose an option:"
    echo ""
    echo "1. Start with default configuration (appsettings.json)"
    echo "2. Start with custom config file"
    echo "3. Start with command-line user setup"
    echo "4. Generate self-signed certificate"
    echo "5. Build project"
    echo "6. View help"
    echo "7. Exit"
    echo ""
}

start_default() {
    echo ""
    echo "Starting server with appsettings.json..."
    echo ""
    dotnet run
}

start_custom() {
    echo ""
    read -p "Enter path to config file: " configfile
    echo "Starting server with $configfile..."
    echo ""
    dotnet run -- --config "$configfile"
}

start_cmdline() {
    echo ""
    echo "Example: --ip 0.0.0.0 --port 2121 --user admin:pass:/root:RWDCXN"
    read -p "Enter command-line arguments: " args
    echo "Starting server..."
    echo ""
    dotnet run -- $args
}

gen_cert() {
    echo ""
    echo "Generating self-signed certificate..."
    echo ""
    
    # Check if OpenSSL is available
    if ! command -v openssl &> /dev/null; then
        echo "Error: OpenSSL is not installed."
        echo "Install it using:"
        echo "  Ubuntu/Debian: sudo apt-get install openssl"
        echo "  CentOS/RHEL: sudo yum install openssl"
        echo "  macOS: brew install openssl"
        return
    fi
    
    # Generate certificate
    openssl req -x509 -newkey rsa:4096 -keyout server.key -out server.crt \
        -days 365 -nodes -subj "/CN=localhost"
    
    # Convert to PFX format
    read -s -p "Enter password for certificate (or press Enter for no password): " certpass
    echo ""
    
    if [ -z "$certpass" ]; then
        openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt \
            -passout pass:
    else
        openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt \
            -passout pass:$certpass
    fi
    
    echo ""
    echo "Certificate generated successfully!"
    echo "  Key: server.key"
    echo "  Certificate: server.crt"
    echo "  PFX: server.pfx"
    echo ""
    
    # Clean up individual files (optional)
    read -p "Delete key and crt files (keep only pfx)? (y/N): " cleanup
    if [ "$cleanup" = "y" ] || [ "$cleanup" = "Y" ]; then
        rm server.key server.crt
        echo "Cleaned up temporary files."
    fi
    
    echo ""
    read -p "Press Enter to continue..."
}

build_project() {
    echo ""
    echo "Building project..."
    echo ""
    dotnet restore
    dotnet build -c Release
    echo ""
    echo "Build complete!"
    echo ""
    read -p "Press Enter to continue..."
}

show_help() {
    echo ""
    echo "Command-line options:"
    echo "  --config <path>              Path to JSON configuration file"
    echo "  --ip <address>               IP address to bind"
    echo "  --port <number>              Port number"
    echo "  --root <path>                Root directory path"
    echo "  --cert <path>                Certificate file path (.pfx)"
    echo "  --certpass <password>        Certificate password"
    echo "  --user <name:pass:folder:perms>  Add user"
    echo ""
    echo "User permission format: username:password:folder:RWDCXN"
    echo "  R = Read, W = Write, D = Delete"
    echo "  C = Create directories, X = Delete directories"
    echo "  N = Rename files/directories"
    echo ""
    echo "Examples:"
    echo "  --user admin:pass123:/home/admin:RWDCXN"
    echo "  --user readonly:view:/public:R"
    echo ""
    echo "Full example:"
    echo "  dotnet run -- --ip 0.0.0.0 --port 2121 \\"
    echo "    --cert server.pfx --certpass mypass \\"
    echo "    --user admin:admin123:/admin:RWDCXN \\"
    echo "    --user user:user123:/users/user:RWC"
    echo ""
    read -p "Press Enter to continue..."
}

# Main loop
while true; do
    show_menu
    read -p "Enter your choice (1-7): " choice
    
    case $choice in
        1)
            start_default
            break
            ;;
        2)
            start_custom
            break
            ;;
        3)
            start_cmdline
            break
            ;;
        4)
            gen_cert
            ;;
        5)
            build_project
            ;;
        6)
            show_help
            ;;
        7)
            echo ""
            echo "Goodbye!"
            exit 0
            ;;
        *)
            echo ""
            echo "Invalid choice. Please try again."
            echo ""
            ;;
    esac
done
