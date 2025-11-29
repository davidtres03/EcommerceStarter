#!/usr/bin/env pwsh
# Install-WindowsService.ps1 - Install EcommerceStarter Background Service

param(
    [Parameter(Mandatory = $true)]
    [string]$ServicePath,

    [string]$ServiceName = "EcommerceStarter-Background-Service",
    [string]$DisplayName = "EcommerceStarter Background Service",
    [string]$Description = "Background service for EcommerceStarter - monitors health, checks for updates, and manages auto-restarts"
)

# Require admin privileges
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "This script requires administrator privileges. Restarting with elevation..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"& '$PSCommandPath' -ServicePath '$ServicePath'`"" -Verb RunAs
    exit
}

Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "EcommerceStarter Background Service Installer" -ForegroundColor Cyan
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if service path exists
if (-not (Test-Path $ServicePath)) {
    Write-Host "ERROR: Service executable not found at: $ServicePath" -ForegroundColor Red
    exit 1
}

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existingService) {
    Write-Host "Service '$ServiceName' already exists" -ForegroundColor Yellow
    Write-Host "Stopping existing service..." -ForegroundColor Gray
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    Write-Host "Removing existing service..." -ForegroundColor Gray
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Create new service
Write-Host "Creating new Windows Service..." -ForegroundColor Green
Write-Host "  Name: $ServiceName" -ForegroundColor Gray
Write-Host "  Display Name: $DisplayName" -ForegroundColor Gray
Write-Host "  Path: $ServicePath" -ForegroundColor Gray

New-Service -Name $ServiceName `
    -DisplayName $DisplayName `
    -Description $Description `
    -BinaryPathName "$ServicePath --service" `
    -StartupType Automatic `
    -ErrorAction Stop | Out-Null

Write-Host "✓ Service created successfully" -ForegroundColor Green

# Configure service recovery
Write-Host "Configuring service recovery..." -ForegroundColor Gray
sc.exe failure $ServiceName reset=3600 actions=restart/60000/restart/120000/none | Out-Null

# Start the service
Write-Host "Starting service..." -ForegroundColor Gray
Start-Service -Name $ServiceName -ErrorAction Stop

# Verify service is running
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName
if ($service.Status -eq 'Running') {
    Write-Host "✓ Service is running" -ForegroundColor Green
}
else {
    Write-Host "✗ Service failed to start" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Installation completed successfully!" -ForegroundColor Green
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service information:" -ForegroundColor Cyan
Write-Host "  Name: $ServiceName" -ForegroundColor Gray
Write-Host "  Status: Running" -ForegroundColor Green
Write-Host "  Startup Type: Automatic" -ForegroundColor Gray
Write-Host "  Recovery: Restart after 60 seconds" -ForegroundColor Gray
Write-Host ""
Write-Host "To manage the service, use:" -ForegroundColor Cyan
Write-Host "  Get-Service '$ServiceName'" -ForegroundColor Gray
Write-Host "  Stop-Service '$ServiceName'" -ForegroundColor Gray
Write-Host "  Start-Service '$ServiceName'" -ForegroundColor Gray
Write-Host "  Remove-Service '$ServiceName' (uninstall)" -ForegroundColor Gray
