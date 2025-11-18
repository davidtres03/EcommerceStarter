# Remove EcommerceStarter from Programs and Features
# RUN THIS AS ADMINISTRATOR!

Write-Host "`n?? Removing EcommerceStarter Registry Entries`n" -ForegroundColor Cyan

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "? ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "`nRight-click this file and select 'Run as Administrator'`n" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Removing registry entries..." -ForegroundColor Yellow

try {
    # Remove both entries
    Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_CapAndCollar" -Force -ErrorAction Stop
    Write-Host "  ? Removed: EcommerceStarter_CapAndCollar" -ForegroundColor Green
} catch {
    Write-Host "  ??  Entry not found or already removed: EcommerceStarter_CapAndCollar" -ForegroundColor Gray
}

try {
    Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_CapAndCollarSupplyCo." -Force -ErrorAction Stop
    Write-Host "  ? Removed: EcommerceStarter_CapAndCollarSupplyCo." -ForegroundColor Green
} catch {
    Write-Host "  ??  Entry not found or already removed: EcommerceStarter_CapAndCollarSupplyCo." -ForegroundColor Gray
}

Write-Host "`n? Registry cleanup complete!" -ForegroundColor Green
Write-Host "`nEcommerceStarter should no longer appear in Programs and Features.`n" -ForegroundColor Cyan

pause
