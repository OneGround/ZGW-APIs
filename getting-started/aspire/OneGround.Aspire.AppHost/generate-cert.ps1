# Copy SSL Certificate from Docker-Compose Setup for HAProxy

$certDir = "$PSScriptRoot\haproxy\certs"
$certFile = "$certDir\oneground.local.pem"

# Check if certificate already exists
if (Test-Path $certFile) {
    Write-Host "Certificate already exists at: $certFile" -ForegroundColor Green
    exit 0
}

Write-Host "Setting up SSL certificate for *.oneground.local..." -ForegroundColor Yellow

# Create directory if it doesn't exist
if (!(Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
}

# Path to existing certificates from docker-compose setup
$dockerComposeCertDir = Join-Path $PSScriptRoot "..\..\..\localdev\oneground-certificates"
$sourceCertFile = Join-Path $dockerComposeCertDir "oneground.local.combined.pem"

# Check if docker-compose certificates exist
if (Test-Path $sourceCertFile) {
    Write-Host "Using existing certificate from docker-compose setup..." -ForegroundColor Cyan
    Copy-Item -Path $sourceCertFile -Destination $certFile -Force
    Write-Host "Certificate copied successfully!" -ForegroundColor Green
    Write-Host "Source: $sourceCertFile" -ForegroundColor Gray
    Write-Host "Destination: $certFile" -ForegroundColor Gray
    exit 0
}

Write-Host "Existing certificates not found in docker-compose setup." -ForegroundColor Yellow
Write-Host "Checking for alternative certificate formats..." -ForegroundColor Yellow

# Try to create combined PEM from separate files
$crtFile = Join-Path $dockerComposeCertDir "oneground.local.crt"
$keyFile = Join-Path $dockerComposeCertDir "oneground.local.key"

if ((Test-Path $crtFile) -and (Test-Path $keyFile)) {
    Write-Host "Creating combined certificate from .crt and .key files..." -ForegroundColor Cyan
    Get-Content $crtFile, $keyFile | Set-Content $certFile
    Write-Host "Certificate created successfully!" -ForegroundColor Green
    Write-Host "Location: $certFile" -ForegroundColor Cyan
    exit 0
}

# If we get here, no certificates found - provide instructions
Write-Host ""
Write-Host "No existing certificates found. Please generate them first:" -ForegroundColor Red
Write-Host ""
Write-Host "Option 1 - Use the docker-compose setup to generate certificates:" -ForegroundColor Yellow
Write-Host "  cd ..\..\..\localdev" -ForegroundColor Cyan
Write-Host "  docker-compose up -d" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option 2 - Generate manually with Docker:" -ForegroundColor Yellow
Write-Host "  docker run --rm -v `"${certDir}:/certs`" alpine/openssl req -x509 -newkey rsa:2048 -keyout /certs/key.pem -out /certs/cert.pem -days 365 -nodes -subj '/CN=*.oneground.local'" -ForegroundColor Cyan
Write-Host "  docker run --rm -v `"${certDir}:/certs`" alpine sh -c 'cat /certs/cert.pem /certs/key.pem > /certs/oneground.local.pem && rm /certs/cert.pem /certs/key.pem'" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option 3 - Install OpenSSL and run this script again" -ForegroundColor Yellow
Write-Host ""
exit 1
