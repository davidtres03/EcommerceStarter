# Launch EcommerceStarter Installer with Administrator Privileges
# This ensures the installer can write to C:\inetpub\wwwroot and configure IIS

Write-Host "?? Launching EcommerceStarter Installer..." -ForegroundColor Cyan
Write-Host "?? Note: Administrator privileges are required for IIS configuration" -ForegroundColor Yellow
Write-Host ""

$installerPath = ".\EcommerceStarter.Installer\bin\Debug\net8.0-windows\EcommerceStarter.Installer.exe"

if (-not (Test-Path $installerPath)) {
    Write-Host "? Installer not found. Building first..." -ForegroundColor Red
    dotnet build EcommerceStarter.Installer\EcommerceStarter.Installer.csproj --configuration Debug
}

# Launch as administrator
Start-Process $installerPath -Verb RunAs

Write-Host "? Installer launched with admin privileges!" -ForegroundColor Green
