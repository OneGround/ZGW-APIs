# This script installs a certificate into the Trusted Root Certification Authorities
# for the local machine. It must be run with Administrator privileges.

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$certPath = Join-Path -Path $scriptDir -ChildPath "..\oneground-certificates\oneground.local.pem"

Write-Host "Attempting to install certificate from: $certPath"
Write-Host "This requires Administrator privileges."
Write-Host ""

if (-not (Test-Path $certPath)) {
    Write-Error "Certificate file not found at '$certPath'. Please make sure you have generated the certificate using 'docker-compose TODO' first."
} else {
    try {       
        Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root -ErrorAction Stop
        Write-Host "Success! The certificate has been installed into the Trusted Root Certification Authorities store."
        Write-Host "You may need to restart your browser for the changes to take effect."
    } catch {        
        Write-Error "An error occurred during certificate installation:"
        Write-Error $_.Exception.Message
        Write-Host "Please ensure you are running this script with Administrator privileges."
    }
}

Write-Host ""
Write-Host "Press any key to continue..."
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null