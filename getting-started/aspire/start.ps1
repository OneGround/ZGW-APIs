# OneGround Aspire Quick Start Script
param(
    [switch]$FromSource,
    [switch]$SkipCertGeneration
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "          OneGround ZGW APIs - .NET Aspire Setup                 " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking prerequisites..." -ForegroundColor Yellow
Write-Host ""

try {
    $dotnetVersion = dotnet --version
    Write-Host "OK .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR .NET SDK not found. Please install .NET 8.0 SDK or later." -ForegroundColor Red
    exit 1
}

try {
    $dockerVersion = docker --version
    Write-Host "OK Docker: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR Docker not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Checking .NET Aspire workload..." -ForegroundColor Yellow
$workloads = dotnet workload list
if ($workloads -match "aspire") {
    Write-Host "OK .NET Aspire workload is installed" -ForegroundColor Green
} else {
    Write-Host "ERROR .NET Aspire workload not found" -ForegroundColor Red
    Write-Host "Please install: dotnet workload install aspire" -ForegroundColor Yellow
    exit 1
}

if (-not $SkipCertGeneration) {
    Write-Host ""
    Write-Host "Checking SSL certificate..." -ForegroundColor Yellow
    & "$PSScriptRoot\OneGround.Aspire.AppHost\generate-cert.ps1"
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "All prerequisites are satisfied!" -ForegroundColor Green
Write-Host ""

if ($FromSource) {
    Write-Host "Starting OneGround with .NET Aspire (from source code)..." -ForegroundColor Yellow
    Set-Location "$PSScriptRoot\OneGround.Aspire.AppHost"
    dotnet run --launch-profile Development
} else {
    Write-Host "Starting OneGround with .NET Aspire (using Docker images)..." -ForegroundColor Yellow
    Write-Host "Tip: Use -FromSource flag to run from source code instead" -ForegroundColor Cyan
    Write-Host ""
    Set-Location "$PSScriptRoot\OneGround.Aspire.AppHost"
    dotnet run
}
