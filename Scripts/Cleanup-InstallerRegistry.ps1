# Clean Up Old EcommerceStarter Registry Entries
# This removes duplicate/old installations from Windows registry

Write-Host "?? CLEANING UP OLD ECOMMERCESTARTER REGISTRY ENTRIES" -ForegroundColor Cyan
Write-Host ""

# Must run as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "? ERROR: Must run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit
}

Write-Host "Finding all EcommerceStarter installations in registry..." -ForegroundColor Yellow
Write-Host ""

$uninstallPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
$entries = Get-ChildItem $uninstallPath | Where-Object { $_.Name -like "*EcommerceStarter*" }

$validEntries = @()
$invalidEntries = @()

foreach ($entry in $entries) {
    $props = Get-ItemProperty $entry.PSPath
    $installLocation = $props.InstallLocation
    $displayName = $props.DisplayName
    $keyName = $entry.PSChildName
    
    Write-Host "Found: $displayName" -ForegroundColor Cyan
    Write-Host "  Location: $installLocation"
    Write-Host "  Registry Key: $keyName"
    
    # Check if installation path actually exists
    if (Test-Path $installLocation) {
        Write-Host "  ? Installation exists" -ForegroundColor Green
        $validEntries += @{
            KeyName = $keyName
            DisplayName = $displayName
            InstallLocation = $installLocation
            Entry = $entry
        }
    } else {
        Write-Host "  ? Installation path not found (orphaned entry)" -ForegroundColor Red
        $invalidEntries += @{
            KeyName = $keyName
            DisplayName = $displayName
            InstallLocation = $installLocation
            Entry = $entry
        }
    }
    Write-Host ""
}

# Show summary
Write-Host "?? SUMMARY:" -ForegroundColor Yellow
Write-Host "  Valid installations: $($validEntries.Count)"
Write-Host "  Orphaned entries: $($invalidEntries.Count)"
Write-Host ""

# Ask to remove orphaned entries
if ($invalidEntries.Count -gt 0) {
    Write-Host "???  ORPHANED ENTRIES (installation path doesn't exist):" -ForegroundColor Yellow
    foreach ($entry in $invalidEntries) {
        Write-Host "  • $($entry.DisplayName)"
        Write-Host "    Location: $($entry.InstallLocation)"
    }
    Write-Host ""
    
    $response = Read-Host "Remove these orphaned entries? (Y/N)"
    if ($response -eq 'Y' -or $response -eq 'y') {
        foreach ($entry in $invalidEntries) {
            try {
                Remove-Item -Path $entry.Entry.PSPath -Recurse -Force
                Write-Host "  ? Removed: $($entry.DisplayName)" -ForegroundColor Green
            } catch {
                Write-Host "  ? Failed to remove: $($entry.DisplayName)" -ForegroundColor Red
                Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "Skipped removing orphaned entries" -ForegroundColor Yellow
    }
}

# Show remaining valid installations
if ($validEntries.Count -gt 1) {
    Write-Host ""
    Write-Host "??  WARNING: Multiple valid installations found!" -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($i = 0; $i -lt $validEntries.Count; $i++) {
        $entry = $validEntries[$i]
        Write-Host "[$($i + 1)] $($entry.DisplayName)"
        Write-Host "    Location: $($entry.InstallLocation)"
        
        # Check appsettings.json to see database
        $appsettingsPath = Join-Path $entry.InstallLocation "appsettings.json"
        if (Test-Path $appsettingsPath) {
            try {
                $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
                $connString = $json.ConnectionStrings.DefaultConnection
                if ($connString -match "Database=([^;]+)") {
                    Write-Host "    Database: $($matches[1])" -ForegroundColor Cyan
                }
            } catch {
                Write-Host "    Database: Could not read" -ForegroundColor Gray
            }
        }
        Write-Host ""
    }
    
    Write-Host "The installer will use the FIRST entry found." -ForegroundColor Yellow
    Write-Host "If you want to keep only one, manually remove the others from:" -ForegroundColor Yellow
    Write-Host "  HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" -ForegroundColor Gray
}

Write-Host ""
Write-Host "? Cleanup complete!" -ForegroundColor Green
Write-Host ""
pause
