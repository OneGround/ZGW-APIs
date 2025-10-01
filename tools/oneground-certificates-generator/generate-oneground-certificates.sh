#!/bin/sh

set -e

CERT_DIR="/certs"
DOMAIN="${1:-oneground.local}"

CERT_FILE_PEM="${CERT_DIR}/${DOMAIN}.pem"
CERT_FILE_CRT="${CERT_DIR}/${DOMAIN}.crt"
KEY_FILE="${CERT_DIR}/${DOMAIN}.key"
COMBINED_FILE="${CERT_DIR}/${DOMAIN}.combined.pem"

if [ -f "$KEY_FILE" ]; then
  echo "Certificates for ${DOMAIN} already exist. Skipping generation."
  exit 0
fi

echo "Generating certificates for ${DOMAIN} and *.${DOMAIN}..."

# Create certificate directory with proper permissions
mkdir -p "$CERT_DIR"

openssl req -x509 -newkey rsa:4096 \
  -keyout "${KEY_FILE}" \
  -out "${CERT_FILE_PEM}" \
  -sha256 -days 365 -nodes \
  -subj "/CN=${DOMAIN}" \
  -addext "subjectAltName = DNS:${DOMAIN},DNS:*.${DOMAIN}"

cp "${CERT_FILE_PEM}" "${CERT_FILE_CRT}"

cat "${CERT_FILE_PEM}" "${KEY_FILE}" > "${COMBINED_FILE}"

# Set permissions to be readable by all users (for cross-platform compatibility)
chmod 666 "${CERT_FILE_PEM}" "${CERT_FILE_CRT}" "${KEY_FILE}" "${COMBINED_FILE}"

echo "Certificates generated successfully!"
echo "  - Key: ${KEY_FILE}"
echo "  - Certificate (PEM): ${CERT_FILE_PEM}"
echo "  - Certificate (CRT): ${CERT_FILE_CRT}"
echo "  - Combined for HAProxy: ${COMBINED_FILE}"
