# Check EcommerceStarter Database Configuration
# Shows both registry info and actual database connection

Write-Host "?? CHECKING ECOMMERCESTARTER DATABASE CONFIGURATION" -ForegroundColor Cyan
Write-Host ""

# Check Registry
Write-Host "?? Registry Information:" -ForegroundColor Yellow
Write-Host "Location: HKLM:\SOFTWARE\EcommerceStarter"
Write-Host ""

try {
    $regKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\EcommerceStarter" -ErrorAction Stop
    
    Write-Host "  ? Installation Found" -ForegroundColor Green
    Write-Host "  Install Path: $($regKey.InstallPath)"
    Write-Host "  Version: $($regKey.InstalledVersion)"
    Write-Host "  Install Date: $($regKey.InstallDate)"
    Write-Host ""
    
    $installPath = $regKey.InstallPath
    
    # Check appsettings.json
    Write-Host "???  Database Configuration:" -ForegroundColor Yellow
    
    $appsettingsPath = Join-Path $installPath "appsettings.json"
    Write-Host "Looking in: $appsettingsPath"
    Write-Host ""
    
    if (Test-Path $appsettingsPath) {
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $connectionString = $json.ConnectionStrings.DefaultConnection
        
        Write-Host "  ? Configuration File Found" -ForegroundColor Green
        
        # Parse connection string
        if ($connectionString -match "Server=([^;]+)") {
            Write-Host "  Server: $($matches[1])"
        }
        
        if ($connectionString -match "Database=([^;]+)") {
            Write-Host "  Database: $($matches[1])" -ForegroundColor Cyan
        }
        
        if ($connectionString -match "Trusted_Connection=True") {
            Write-Host "  Authentication: Windows Authentication" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "  Full Connection String:"
        Write-Host "  $connectionString" -ForegroundColor Gray
        
    } else {
        Write-Host "  ? appsettings.json not found" -ForegroundColor Red
    }
    
} catch {
    Write-Host "  ? No EcommerceStarter installation found in registry" -ForegroundColor Red
}

Write-Host ""
Write-Host "?? Note: Database connection is stored in appsettings.json, NOT in registry!" -ForegroundColor Yellow
Write-Host "   Registry only stores: Install path, version, and date"
Write-Host ""
