# Remove Orphaned EcommerceStarter Uninstall Registry Entry
# Run this as Administrator to clean up orphaned "Cap And Collar Supply Co" entry

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Remove Orphaned EcommerceStarter Uninstall Entry" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Check admin
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

$uninstallPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"

# Find all EcommerceStarter entries
Write-Host "Searching for EcommerceStarter uninstall entries..." -ForegroundColor Yellow
$found = @()

Get-ChildItem $uninstallPath | ForEach-Object {
    $displayName = $_.GetValue("DisplayName")
    if ($displayName -like "*EcommerceStarter*") {
        $installLocation = $_.GetValue("InstallLocation")
        $uninstallString = $_.GetValue("UninstallString")

        $orphaned = $false
        $reason = ""

        # Check if orphaned
        if ($installLocation -and -not (Test-Path $installLocation)) {
            $orphaned = $true
            $reason = "Install location not found: $installLocation"
        }
        elseif ($uninstallString -match '"([^"]+)"') {
            $uninstallerPath = $matches[1]
            if (-not (Test-Path $uninstallerPath)) {
                $orphaned = $true
                $reason = "Uninstaller not found: $uninstallerPath"
            }
        }

        $found += [PSCustomObject]@{
            DisplayName = $displayName
            KeyPath = $_.PSPath
            KeyName = $_.PSChildName
            InstallLocation = $installLocation
            UninstallString = $uninstallString
            IsOrphaned = $orphaned
            Reason = $reason
        }
    }
}

if ($found.Count -eq 0) {
    Write-Host "No EcommerceStarter entries found in registry." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "Found $($found.Count) EcommerceStarter entries:" -ForegroundColor Cyan
Write-Host ""

for ($i = 0; $i -lt $found.Count; $i++) {
    $entry = $found[$i]
    Write-Host "[$($i+1)] $($entry.DisplayName)" -ForegroundColor $(if ($entry.IsOrphaned) { "Yellow" } else { "Green" })
    Write-Host "    Key: $($entry.KeyName)" -ForegroundColor Gray
    Write-Host "    Location: $($entry.InstallLocation)" -ForegroundColor Gray
    if ($entry.IsOrphaned) {
        Write-Host "    STATUS: ORPHANED - $($entry.Reason)" -ForegroundColor Red
    } else {
        Write-Host "    STATUS: Valid" -ForegroundColor Green
    }
    Write-Host ""
}

$orphaned = $found | Where-Object { $_.IsOrphaned }

if ($orphaned.Count -eq 0) {
    Write-Host "No orphaned entries found. All entries are valid." -ForegroundColor Green
    exit 0
}

Write-Host "Found $($orphaned.Count) orphaned entries." -ForegroundColor Yellow
Write-Host ""
Write-Host "Do you want to remove these orphaned entries? (Y/N): " -ForegroundColor Yellow -NoNewline
$confirm = Read-Host

if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Removing orphaned entries..." -ForegroundColor Cyan

foreach ($entry in $orphaned) {
    try {
        Write-Host "  Removing: $($entry.DisplayName)..." -ForegroundColor Yellow
        Remove-Item -Path $entry.KeyPath -Recurse -Force -ErrorAction Stop
        Write-Host "    ✓ Removed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "    ✗ Failed: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "The orphaned entries have been removed from Programs and Features." -ForegroundColor Green
Write-Host "You can now close this window." -ForegroundColor Gray
Write-Host ""
