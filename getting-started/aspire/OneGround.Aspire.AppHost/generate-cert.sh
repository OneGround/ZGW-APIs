#!/bin/bash

# Copy SSL Certificate from Docker-Compose Setup for HAProxy

CERT_DIR="$(dirname "$0")/haproxy/certs"
CERT_FILE="$CERT_DIR/oneground.local.pem"

# Check if certificate already exists
if [ -f "$CERT_FILE" ]; then
    echo "Certificate already exists at: $CERT_FILE"
    exit 0
fi

echo "Setting up SSL certificate for *.oneground.local..."

# Create directory if it doesn't exist
mkdir -p "$CERT_DIR"

# Path to existing certificates from docker-compose setup
DOCKER_COMPOSE_CERT_DIR="$(dirname "$0")/../../../localdev/oneground-certificates"
SOURCE_CERT_FILE="$DOCKER_COMPOSE_CERT_DIR/oneground.local.combined.pem"

# Check if docker-compose certificates exist
if [ -f "$SOURCE_CERT_FILE" ]; then
    echo "Using existing certificate from docker-compose setup..."
    cp "$SOURCE_CERT_FILE" "$CERT_FILE"
    echo "Certificate copied successfully!"
    echo "Source: $SOURCE_CERT_FILE"
    echo "Destination: $CERT_FILE"
    exit 0
fi

echo "Existing certificates not found in docker-compose setup."
echo "Checking for alternative certificate formats..."

# Try to create combined PEM from separate files
CRT_FILE="$DOCKER_COMPOSE_CERT_DIR/oneground.local.crt"
KEY_FILE="$DOCKER_COMPOSE_CERT_DIR/oneground.local.key"

if [ -f "$CRT_FILE" ] && [ -f "$KEY_FILE" ]; then
    echo "Creating combined certificate from .crt and .key files..."
    cat "$CRT_FILE" "$KEY_FILE" > "$CERT_FILE"
    echo "Certificate created successfully!"
    echo "Location: $CERT_FILE"
    exit 0
fi

# If we get here, no certificates found - provide instructions
echo ""
echo "No existing certificates found. Please generate them first:"
echo ""
echo "Option 1 - Use the docker-compose setup to generate certificates:"
echo "  cd ../../../localdev"
echo "  docker-compose up -d"
echo ""
echo "Option 2 - Generate manually with Docker:"
echo "  docker run --rm -v \"$CERT_DIR:/certs\" alpine/openssl req -x509 -newkey rsa:2048 -keyout /certs/key.pem -out /certs/cert.pem -days 365 -nodes -subj '/CN=*.oneground.local'"
echo "  docker run --rm -v \"$CERT_DIR:/certs\" alpine sh -c 'cat /certs/cert.pem /certs/key.pem > /certs/oneground.local.pem && rm /certs/cert.pem /certs/key.pem'"
echo ""
echo "Option 3 - Install OpenSSL and run this script again"
echo ""
exit 1
