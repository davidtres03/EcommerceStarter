#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Fixes duplicate Programs & Features entries for EcommerceStarter
    
.DESCRIPTION
    This script removes old duplicate registry entries from Programs & Features.
    Run this if you see multiple "EcommerceStarter" entries in Windows Programs & Features.
    
.NOTES
    Must be run as Administrator
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Fix Duplicate Registry Entries" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Registry path for Programs & Features
$uninstallBasePath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"

# Find all EcommerceStarter entries
Write-Host "Searching for EcommerceStarter registry entries..." -ForegroundColor Yellow
$entries = Get-ChildItem -Path $uninstallBasePath | Where-Object {
    $_.PSChildName -like "EcommerceStarter*"
}

if ($entries.Count -eq 0) {
    Write-Host "? No EcommerceStarter entries found in registry." -ForegroundColor Green
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

Write-Host ""
Write-Host "Found $($entries.Count) EcommerceStarter registry entries:" -ForegroundColor White
Write-Host ""

$index = 1
foreach ($entry in $entries) {
    $displayName = (Get-ItemProperty -Path $entry.PSPath -Name DisplayName -ErrorAction SilentlyContinue).DisplayName
    $version = (Get-ItemProperty -Path $entry.PSPath -Name DisplayVersion -ErrorAction SilentlyContinue).DisplayVersion
    $installDate = (Get-ItemProperty -Path $entry.PSPath -Name InstallDate -ErrorAction SilentlyContinue).InstallDate
    
    Write-Host "  [$index] $($entry.PSChildName)" -ForegroundColor Cyan
    Write-Host "      Display Name: $displayName" -ForegroundColor Gray
    Write-Host "      Version: $version" -ForegroundColor Gray
    Write-Host "      Install Date: $installDate" -ForegroundColor Gray
    Write-Host ""
    
    $index++
}

# Check if there are duplicates
if ($entries.Count -gt 1) {
    Write-Host "? WARNING: Found $($entries.Count) entries. Only 1 entry should exist." -ForegroundColor Red
    Write-Host ""
    Write-Host "This usually happens when:" -ForegroundColor Yellow
    Write-Host "  - Multiple installations were performed" -ForegroundColor Yellow
    Write-Host "  - An upgrade was done without proper uninstall" -ForegroundColor Yellow
    Write-Host "  - Registry cleanup was not completed" -ForegroundColor Yellow
    Write-Host ""
    
    $response = Read-Host "Do you want to remove ALL entries? (yes/no)"
    
    if ($response -eq "yes") {
        Write-Host ""
        Write-Host "Removing all EcommerceStarter registry entries..." -ForegroundColor Yellow
        
        foreach ($entry in $entries) {
            try {
                $keyName = $entry.PSChildName
                Remove-Item -Path $entry.PSPath -Recurse -Force -ErrorAction Stop
                Write-Host "  ? Removed: $keyName" -ForegroundColor Green
            }
            catch {
                Write-Host "  ? Failed to remove $($entry.PSChildName): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        
        Write-Host ""
        Write-Host "? Registry cleanup complete!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Note: You may need to refresh Programs & Features (F5) to see the changes." -ForegroundColor Cyan
    }
    else {
        Write-Host ""
        Write-Host "Operation cancelled. No changes were made." -ForegroundColor Yellow
    }
}
else {
    Write-Host "? Only 1 entry found. No duplicates detected." -ForegroundColor Green
}

Write-Host ""
Write-Host "Additional cleanup:" -ForegroundColor Cyan
Write-Host ""

# Check for custom tracking registry
$customRegPath = "HKLM:\SOFTWARE\EcommerceStarter"
if (Test-Path $customRegPath) {
    Write-Host "Found custom tracking registry at: SOFTWARE\EcommerceStarter" -ForegroundColor Yellow
    $removeCustom = Read-Host "Remove custom tracking registry? (yes/no)"
    
    if ($removeCustom -eq "yes") {
        try {
            Remove-Item -Path $customRegPath -Recurse -Force -ErrorAction Stop
            Write-Host "  ? Removed custom tracking registry" -ForegroundColor Green
        }
        catch {
            Write-Host "  ? Failed to remove: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "? No custom tracking registry found." -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Cleanup Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
