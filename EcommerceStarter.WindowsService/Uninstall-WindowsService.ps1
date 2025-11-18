#!/usr/bin/env pwsh
# Uninstall-WindowsService.ps1 - Remove EcommerceStarter Background Service

param(
    [string]$ServiceName = "EcommerceStarter-Background-Service"
)

# Require admin privileges
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "This script requires administrator privileges. Restarting with elevation..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"& '$PSCommandPath'`"" -Verb RunAs
    exit
}

Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "EcommerceStarter Background Service Uninstaller" -ForegroundColor Cyan
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Host "Service '$ServiceName' not found" -ForegroundColor Yellow
    exit 0
}

# Stop the service if running
Write-Host "Stopping service..." -ForegroundColor Gray
if ($service.Status -eq 'Running') {
    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
    Start-Sleep -Seconds 2
    Write-Host "✓ Service stopped" -ForegroundColor Green
} else {
    Write-Host "✓ Service already stopped" -ForegroundColor Gray
}

# Remove the service
Write-Host "Removing service registration..." -ForegroundColor Gray
sc.exe delete $ServiceName
Start-Sleep -Seconds 2

# Verify removal
$serviceAfter = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $serviceAfter) {
    Write-Host "✓ Service removed successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Service removal failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Uninstallation completed successfully!" -ForegroundColor Green
Write-Host "═════════════════════════════════════════════════════════════" -ForegroundColor Cyan
