#!/bin/bash

set -e

if [[ -z "$1" ]]; then
    echo "Error: No certificate path provided." >&2
    echo "Usage: $0 <path-to-certificate-file>" >&2
    exit 1
fi

CERT_FILE="$1"
CERT_NAME="$(basename "$CERT_FILE" .pem).crt"

if [[ ! -f "$CERT_FILE" ]]; then
    echo "Certificate file not found at '$CERT_FILE'! Please check the path." >&2
    exit 1
fi

echo "Installing certificate for macOS/Linux..."

if [[ "$(uname)" == "Darwin" ]]; then
    echo "Detected macOS..."
    security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain "$CERT_FILE"
    echo "Certificate installed in macOS System Keychain."
elif [[ "$(uname)" == "Linux" ]]; then
    echo "Detected Linux..."

    if command -v update-ca-certificates &> /dev/null; then
        cp "$CERT_FILE" "/usr/local/share/ca-certificates/$CERT_NAME"
        update-ca-certificates
        echo "Certificate installed for Debian/Ubuntu-based systems."
    elif command -v update-ca-trust &> /dev/null; then
        cp "$CERT_FILE" "/etc/pki/ca-trust/source/anchors/"
        update-ca-trust
        echo "Certificate installed for RHEL-based systems."
    else
        echo "Could not find a known certificate management tool (update-ca-certificates or update-ca-trust)." >&2
        exit 1
    fi
else
    echo "Unsupported operating system: $(uname)" >&2
    exit 1
fi

echo "Certificate installation complete. You may need to restart your browser."