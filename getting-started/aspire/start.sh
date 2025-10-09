#!/bin/bash

# OneGround Aspire Quick Start Script
# This script helps you get started with OneGround using .NET Aspire

FROM_SOURCE=false
SKIP_CERT=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --from-source)
            FROM_SOURCE=true
            shift
            ;;
        --skip-cert)
            SKIP_CERT=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: ./start.sh [--from-source] [--skip-cert]"
            exit 1
            ;;
    esac
done

echo ""
echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║                                                               ║"
echo "║          OneGround ZGW APIs - .NET Aspire Setup               ║"
echo "║                                                               ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""

# Check prerequisites
echo "Checking prerequisites..."
echo ""

# Check .NET SDK
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✓ .NET SDK: $DOTNET_VERSION"
else
    echo "✗ .NET SDK not found. Please install .NET 8.0 SDK or later."
    echo "  Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check Docker
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version)
    echo "✓ Docker: $DOCKER_VERSION"
else
    echo "✗ Docker not found. Please install Docker Desktop."
    echo "  Download from: https://www.docker.com/products/docker-desktop/"
    exit 1
fi

# Check Aspire workload
echo ""
echo "Checking .NET Aspire workload..."
if dotnet workload list | grep -q "aspire"; then
    echo "✓ .NET Aspire workload is installed"
else
    echo "✗ .NET Aspire workload not found"
    echo ""
    read -p "Would you like to install it now? (Y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Installing .NET Aspire workload..."
        dotnet workload update
        dotnet workload install aspire
        echo "✓ .NET Aspire workload installed successfully"
    else
        echo "Please install the Aspire workload manually: dotnet workload install aspire"
        exit 1
    fi
fi

# Generate SSL certificate if needed
if [ "$SKIP_CERT" = false ]; then
    echo ""
    echo "Checking SSL certificate..."
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    bash "$SCRIPT_DIR/OneGround.Aspire.AppHost/generate-cert.sh"
fi

# Check hosts file
echo ""
echo "Checking hosts file configuration..."
HOSTS_FILE="/etc/hosts"

REQUIRED_HOSTS=(
    "zaken.oneground.local"
    "catalogi.oneground.local"
    "besluiten.oneground.local"
    "documenten.oneground.local"
    "autorisaties.oneground.local"
    "notificaties.oneground.local"
    "referentielijsten.oneground.local"
    "keycloak.oneground.local"
)

MISSING_HOSTS=()
for host in "${REQUIRED_HOSTS[@]}"; do
    if ! grep -q "$host" "$HOSTS_FILE" 2>/dev/null; then
        MISSING_HOSTS+=("$host")
    fi
done

if [ ${#MISSING_HOSTS[@]} -gt 0 ]; then
    echo "⚠ The following hosts are not configured in your hosts file:"
    for host in "${MISSING_HOSTS[@]}"; do
        echo "  - $host"
    done
    echo ""
    echo "Please add these entries to $HOSTS_FILE (requires sudo):"
    echo ""
    for host in "${MISSING_HOSTS[@]}"; do
        echo "  127.0.0.1 $host"
    done
    echo ""
    echo "You can continue without this, but you'll need to use localhost:port instead."
    echo ""
else
    echo "✓ All required hosts are configured"
fi

# Ready to run
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "All prerequisites are satisfied!"
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/OneGround.Aspire.AppHost"

if [ "$FROM_SOURCE" = true ]; then
    echo "Starting OneGround with .NET Aspire (from source code)..."
    echo ""
    dotnet run --launch-profile Development
else
    echo "Starting OneGround with .NET Aspire (using Docker images)..."
    echo ""
    echo "Tip: Use --from-source flag to run from source code instead"
    echo ""
    dotnet run
fi
