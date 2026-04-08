#!/bin/sh
# =============================================================================
# Generate a self-signed X.509 certificate for ASP.NET Core DataProtection
#
# This certificate is used by ZakenDataProtectionExtensions.AddZakenDataProtection()
# to encrypt DataProtection keys at rest in the database.
#
# Output: Base64-encoded PFX string + password, ready for configuration:
#   - DataProtection:Certificate     (base64 PFX)
#   - DataProtection:CertificatePassword
#
# Usage (run from the localdev directory):
#   chmod +x ../tools/oneground-certificates-generator/generate-dataprotection-certificate.sh
#   ../tools/oneground-certificates-generator/generate-dataprotection-certificate.sh [password] [days]
#
# Examples:
#   ../tools/oneground-certificates-generator/generate-dataprotection-certificate.sh
#   ../tools/oneground-certificates-generator/generate-dataprotection-certificate.sh MyPassword 1825
# =============================================================================

set -e

CERT_PASSWORD="${1:-DataProtection-LocalDev!}"
CERT_DAYS="${2:-3650}"
CERT_DIR="$(mktemp -d)"
CERT_SUBJECT="/CN=OneGround-DataProtection"

echo "Generating DataProtection certificate (valid for ${CERT_DAYS} days)..."

# Generate RSA key + self-signed certificate
openssl req -x509 -newkey rsa:4096 \
  -keyout "${CERT_DIR}/dp-key.pem" \
  -out "${CERT_DIR}/dp-cert.pem" \
  -sha256 -days "${CERT_DAYS}" -nodes \
  -subj "${CERT_SUBJECT}" 2>/dev/null

# Convert to PFX (PKCS#12)
openssl pkcs12 -export \
  -in "${CERT_DIR}/dp-cert.pem" \
  -inkey "${CERT_DIR}/dp-key.pem" \
  -out "${CERT_DIR}/dataprotection.pfx" \
  -passout "pass:${CERT_PASSWORD}" 2>/dev/null

# Base64 encode the PFX
CERT_BASE64=$(base64 -w 0 "${CERT_DIR}/dataprotection.pfx" 2>/dev/null || base64 -i "${CERT_DIR}/dataprotection.pfx" 2>/dev/null)

# Clean up temp files
rm -f "${CERT_DIR}/dp-key.pem" "${CERT_DIR}/dp-cert.pem" "${CERT_DIR}/dataprotection.pfx"
rmdir "${CERT_DIR}"

echo ""
echo "=== DataProtection Certificate Generated ==="
echo ""
echo "Password: ${CERT_PASSWORD}"
echo ""
echo "Base64 PFX:"
echo "${CERT_BASE64}"
echo ""
echo "=== Configuration ==="
echo ""
echo "For Docker (environment variable):"
echo "  DataProtection__Certificate=${CERT_BASE64}"
echo "  DataProtection__CertificatePassword=${CERT_PASSWORD}"
echo ""
echo "For appsettings.json:"
echo "  \"DataProtection\": {"
echo "    \"Certificate\": \"${CERT_BASE64}\","
echo "    \"CertificatePassword\": \"${CERT_PASSWORD}\""
echo "  }"
echo ""
echo "WARNING: Back up this certificate. If lost, all encrypted DataProtection"
echo "         keys in the database become permanently unreadable."
