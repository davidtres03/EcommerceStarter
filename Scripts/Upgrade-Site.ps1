#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Upgrade EcommerceStarter to latest version
    
.DESCRIPTION
    Safely upgrades an existing installation by:
    - Stopping IIS App Pool
    - Backing up current files
    - Deploying new code
    - Running database migrations
    - Restarting IIS
    
.PARAMETER SiteName
    Name of the site (e.g., "CapAndCollar", "MyStore")
    
.PARAMETER SkipBackup
    Skip backing up existing files (not recommended)
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$SiteName = "CapAndCollar",
    
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EcommerceStarter Upgrade Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$installPath = "C:\inetpub\wwwroot\$SiteName"
$projectPath = "C:\Dev\Websites\EcommerceStarter\EcommerceStarter.csproj"
$backupPath = "C:\Backups\EcommerceStarter\$SiteName-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Verify installation exists
if (-not (Test-Path $installPath)) {
    Write-Host "ERROR: Installation not found at: $installPath" -ForegroundColor Red
    exit 1
}

Write-Host "Site: $SiteName" -ForegroundColor Yellow
Write-Host "Path: $installPath" -ForegroundColor Yellow
Write-Host ""

# Step 1: Stop IIS App Pool
Write-Host "1. Stopping IIS App Pool '$SiteName'..." -ForegroundColor Cyan
try {
    Import-Module WebAdministration
    Stop-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Write-Host "   App pool stopped" -ForegroundColor Green
} catch {
    Write-Host "   Warning: Could not stop app pool: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 2: Backup existing files
if (-not $SkipBackup) {
    Write-Host ""
    Write-Host "2. Backing up existing files..." -ForegroundColor Cyan
    try {
        New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
        
        # Backup everything except logs and uploads
        $excludeDirs = @("logs", "wwwroot\uploads")
        
        Get-ChildItem $installPath -Recurse | ForEach-Object {
            $relativePath = $_.FullName.Substring($installPath.Length)
            $shouldExclude = $false
            
            foreach ($exclude in $excludeDirs) {
                if ($relativePath -like "*$exclude*") {
                    $shouldExclude = $true
                    break
                }
            }
            
            if (-not $shouldExclude) {
                $destPath = Join-Path $backupPath $relativePath
                $destDir = Split-Path $destPath -Parent
                
                if (-not (Test-Path $destDir)) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                }
                
                if ($_.PSIsContainer -eq $false) {
                    Copy-Item $_.FullName $destPath -Force
                }
            }
        }
        
        Write-Host "   Backup created: $backupPath" -ForegroundColor Green
    } catch {
        Write-Host "   Warning: Backup failed: $($_.Exception.Message)" -ForegroundColor Yellow
        $continue = Read-Host "Continue without backup? (yes/no)"
        if ($continue -ne "yes") {
            Write-Host "Upgrade cancelled" -ForegroundColor Red
            Start-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
            exit 1
        }
    }
} else {
    Write-Host ""
    Write-Host "2. Skipping backup (as requested)" -ForegroundColor Yellow
}

# Step 3: Save important config files
Write-Host ""
Write-Host "3. Preserving configuration files..." -ForegroundColor Cyan
$appsettingsPath = Join-Path $installPath "appsettings.json"
$webConfigPath = Join-Path $installPath "web.config"
$tempConfigPath = Join-Path $env:TEMP "upgrade-config-$SiteName"

New-Item -ItemType Directory -Path $tempConfigPath -Force | Out-Null

if (Test-Path $appsettingsPath) {
    Copy-Item $appsettingsPath (Join-Path $tempConfigPath "appsettings.json") -Force
    Write-Host "   Saved appsettings.json" -ForegroundColor Green
}

if (Test-Path $webConfigPath) {
    Copy-Item $webConfigPath (Join-Path $tempConfigPath "web.config") -Force
    Write-Host "   Saved web.config" -ForegroundColor Green
}

# Step 4: Publish new code
Write-Host ""
Write-Host "4. Publishing updated code..." -ForegroundColor Cyan
try {
    $publishOutput = dotnet publish $projectPath -c Release -o $installPath --no-restore 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ERROR: Publish failed!" -ForegroundColor Red
        Write-Host $publishOutput -ForegroundColor Red
        
        Write-Host ""
        Write-Host "Rolling back..." -ForegroundColor Yellow
        Start-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
        exit 1
    }
    
    Write-Host "   Code published successfully" -ForegroundColor Green
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Start-WebAppPool -Name $SiteName -ErrorAction SilentlyContinue
    exit 1
}

# Step 5: Restore config files
Write-Host ""
Write-Host "5. Restoring configuration files..." -ForegroundColor Cyan

$savedAppsettings = Join-Path $tempConfigPath "appsettings.json"
$savedWebConfig = Join-Path $tempConfigPath "web.config"

if (Test-Path $savedAppsettings) {
    Copy-Item $savedAppsettings $appsettingsPath -Force
    Write-Host "   Restored appsettings.json" -ForegroundColor Green
}

if (Test-Path $savedWebConfig) {
    Copy-Item $savedWebConfig $webConfigPath -Force
    Write-Host "   Restored web.config" -ForegroundColor Green
}

# Clean up temp config
Remove-Item $tempConfigPath -Recurse -Force -ErrorAction SilentlyContinue

# Step 6: Run database migrations (if any)
Write-Host ""
Write-Host "6. Running database migrations..." -ForegroundColor Cyan
try {
    Push-Location (Split-Path $projectPath)
    $migrationOutput = dotnet ef database update --context ApplicationDbContext 2>&1
    Pop-Location
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Database up to date" -ForegroundColor Green
    } else {
        Write-Host "   Warning: Migration may have failed" -ForegroundColor Yellow
        Write-Host "   $migrationOutput" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Warning: Could not run migrations: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 7: Start IIS App Pool
Write-Host ""
Write-Host "7. Starting IIS App Pool..." -ForegroundColor Cyan
try {
    Start-WebAppPool -Name $SiteName
    Start-Sleep -Seconds 2
    
    $poolState = Get-WebAppPoolState -Name $SiteName
    if ($poolState.Value -eq "Started") {
        Write-Host "   App pool started successfully" -ForegroundColor Green
    } else {
        Write-Host "   Warning: App pool state: $($poolState.Value)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Warning: Could not start app pool: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 8: Verify site is responding
Write-Host ""
Write-Host "8. Verifying site..." -ForegroundColor Cyan
Start-Sleep -Seconds 3

try {
    $response = Invoke-WebRequest -Uri "http://localhost/$SiteName" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "   Site is responding (HTTP 200)" -ForegroundColor Green
    } else {
        Write-Host "   Warning: Site returned HTTP $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Warning: Could not verify site: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   Check manually at: http://localhost/$SiteName" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Upgrade Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Site: http://localhost/$SiteName" -ForegroundColor White
if (-not $SkipBackup) {
    Write-Host "  Backup: $backupPath" -ForegroundColor White
}
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test your site at http://localhost/$SiteName" -ForegroundColor White
Write-Host "  2. Log in and verify everything works" -ForegroundColor White
Write-Host "  3. If issues occur, restore from backup" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
