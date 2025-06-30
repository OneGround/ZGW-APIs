#!/bin/sh

set -e

CERT_DIR="/certs"
DOMAIN="oneground.local"

CERT_FILE="${CERT_DIR}/${DOMAIN}.pem"
KEY_FILE="${CERT_DIR}/${DOMAIN}.key"
COMBINED_FILE="${CERT_DIR}/${DOMAIN}.combined.pem"

if [ -f "$KEY_FILE" ]; then
  echo "Certificates for ${DOMAIN} already exist. Skipping generation."
  exit 0
fi

echo "Generating certificates for ${DOMAIN} and *.${DOMAIN}..."

mkdir -p "$CERT_DIR"

openssl req -x509 -newkey rsa:4096 \
  -keyout "${KEY_FILE}" \
  -out "${CERT_FILE}" \
  -sha256 -days 365 -nodes \
  -subj "/CN=${DOMAIN}" \
  -addext "subjectAltName = DNS:${DOMAIN},DNS:*.${DOMAIN}"

cat "${CERT_FILE}" "${KEY_FILE}" > "${COMBINED_FILE}"

echo "Certificates generated successfully!"
echo "  - Key: ${KEY_FILE}"
echo "  - Certificate: ${CERT_FILE}"
echo "  - Combined for HAProxy: ${COMBINED_FILE}"