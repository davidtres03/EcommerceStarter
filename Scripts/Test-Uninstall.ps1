#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Test and debug the uninstaller
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Uninstall Diagnostics" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking current installation state..." -ForegroundColor Yellow
Write-Host ""

# 1. Check Registry
Write-Host "1. Registry Entries:" -ForegroundColor Cyan
$customReg = Get-ItemProperty -Path "HKLM:\SOFTWARE\EcommerceStarter" -ErrorAction SilentlyContinue
if ($customReg) {
    Write-Host "   ? Custom tracking registry found" -ForegroundColor Green
    Write-Host "     Version: $($customReg.InstalledVersion)" -ForegroundColor Gray
    Write-Host "     Path: $($customReg.InstallPath)" -ForegroundColor Gray
} else {
    Write-Host "   ? Custom tracking registry NOT found" -ForegroundColor Red
}

$uninstallReg = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" | 
    Where-Object { $_.PSChildName -like '*EcommerceStarter*' }
if ($uninstallReg) {
    Write-Host "   ? Programs & Features entries found: $($uninstallReg.Count)" -ForegroundColor Green
    foreach ($reg in $uninstallReg) {
        $props = Get-ItemProperty $reg.PSPath
        Write-Host "     - $($props.DisplayName)" -ForegroundColor Gray
    }
} else {
    Write-Host "   ? No Programs & Features entries" -ForegroundColor Red
}

Write-Host ""

# 2. Check Files
Write-Host "2. Installation Files:" -ForegroundColor Cyan
$installPath = "C:\inetpub\wwwroot\MyStore"
if (Test-Path $installPath) {
    $fileCount = (Get-ChildItem $installPath -Recurse -File).Count
    Write-Host "   ? Files exist at: $installPath" -ForegroundColor Green
    Write-Host "     File count: $fileCount" -ForegroundColor Gray
} else {
    Write-Host "   ? No files found at: $installPath" -ForegroundColor Red
}

Write-Host ""

# 3. Check IIS
Write-Host "3. IIS Configuration:" -ForegroundColor Cyan
try {
    Import-Module WebAdministration -ErrorAction Stop
    
    # Check Application Pool
    $appPool = Get-WebAppPoolState -Name "MyStore" -ErrorAction SilentlyContinue
    if ($appPool) {
        Write-Host "   ? Application Pool 'MyStore' exists - State: $($appPool.Value)" -ForegroundColor Green
    } else {
        Write-Host "   ? Application Pool 'MyStore' NOT found" -ForegroundColor Red
    }
    
    # Check Web Application
    $webApp = Get-WebApplication -Name "MyStore" -Site "Default Web Site" -ErrorAction SilentlyContinue
    if ($webApp) {
        Write-Host "   ? Web Application '/MyStore' exists under Default Web Site" -ForegroundColor Green
        Write-Host "     Physical Path: $($webApp.PhysicalPath)" -ForegroundColor Gray
        Write-Host "     App Pool: $($webApp.ApplicationPool)" -ForegroundColor Gray
    } else {
        Write-Host "   ? Web Application '/MyStore' NOT found" -ForegroundColor Red
    }
} catch {
    Write-Host "   ? Cannot check IIS (WebAdministration module not available)" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host ""

# 4. Check Database
Write-Host "4. Database:" -ForegroundColor Cyan
try {
    $result = sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT name FROM sys.databases WHERE name = 'MyStore'" -h -1 2>&1
    if ($result -like "*MyStore*") {
        Write-Host "   ? Database 'MyStore' exists" -ForegroundColor Green
    } else {
        Write-Host "   ? Database 'MyStore' NOT found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ? Cannot check database (sqlcmd not available)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Offer to run uninstall
$response = Read-Host "Do you want to run the uninstaller now? (yes/no)"
if ($response -eq 'yes') {
    Write-Host ""
    Write-Host "Launching uninstaller..." -ForegroundColor Yellow
    
    $installerPath = "C:\Dev\Websites\EcommerceStarter.Installer\bin\Debug\net8.0-windows\EcommerceStarter.Installer.exe"
    if (Test-Path $installerPath) {
        Start-Process -FilePath $installerPath -ArgumentList "--uninstall" -Verb RunAs
    } else {
        Write-Host "? Installer not found at: $installerPath" -ForegroundColor Red
        Write-Host "Build the installer first: dotnet build EcommerceStarter.Installer" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
