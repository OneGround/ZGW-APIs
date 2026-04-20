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
#   Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
#   ..\tools\oneground-certificates-generator\generate-dataprotection-certificate.ps1
#   ..\tools\oneground-certificates-generator\generate-dataprotection-certificate.ps1 -Password "MyPassword" -Days 1825
# =============================================================================

param(
    [string]$Password = "DataProtection-LocalDev!",
    [int]$Days = 3650
)

$ErrorActionPreference = "Stop"

Write-Host "Generating DataProtection certificate (valid for $Days days)..."

$securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

$cert = New-SelfSignedCertificate `
    -Subject "CN=OneGround-DataProtection" `
    -KeyUsage DataEncipherment, KeyEncipherment `
    -KeyAlgorithm RSA `
    -KeyLength 4096 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddDays($Days)

$pfxBytes = $cert.Export(
    [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx,
    $securePassword
)
$certBase64 = [Convert]::ToBase64String($pfxBytes)

# Remove from cert store (we only need the base64 output)
Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== DataProtection Certificate Generated ==="
Write-Host ""
Write-Host "Thumbprint: $($cert.Thumbprint)"
Write-Host "Password:   $Password"
Write-Host ""
Write-Host "=== Configuration ==="
Write-Host ""
Write-Host "For Docker (environment variable):"
Write-Host "  DataProtection__Certificate=$certBase64"
Write-Host "  DataProtection__CertificatePassword=$Password"
Write-Host ""
Write-Host "For appsettings.json:"
Write-Host "  `"DataProtection`": {"
Write-Host "    `"Certificate`": `"$certBase64`","
Write-Host "    `"CertificatePassword`": `"$Password`""
Write-Host "  }"
Write-Host ""
Write-Host "WARNING: Back up this certificate. If lost, all encrypted DataProtection"
Write-Host "         keys in the database become permanently unreadable."
