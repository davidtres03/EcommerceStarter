# ====================================================================
# MyStore Supply Co. - Universal Deployment Script
# ====================================================================
# Reusable script for all deployments - handles all changes automatically
# Safe to run repeatedly - idempotent operations
# ====================================================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Development', 'Staging', 'Production')]
    [string]$Environment = 'Production',
    
    [Parameter(Mandatory=$false)]
    [string]$DeployPath = "C:\inetpub\wwwroot\MyStore",
    
    [Parameter(Mandatory=$false)]
    [string]$SiteName = "EcommerceStarter",
    
    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = "MyStoreAppPool",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 80,
    
    [Parameter(Mandatory=$false)]
    [int]$HttpsPort = 443,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBackup,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$QuickDeploy  # Skip IIS config if already set up
)

# ====================================================================
# Configuration
# ====================================================================

$ErrorActionPreference = "Stop"

# Determine repository root based on script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = if ($ScriptDir -like "*\Scripts*") {
    # Script is in Scripts folder, go up one level
    Split-Path -Parent $ScriptDir
} else {
    # Script is in root
    $ScriptDir
}

# Project paths relative to repository root
$ProjectDir = Join-Path $RepoRoot "EcommerceStarter"
$ProjectFile = Join-Path $ProjectDir "EcommerceStarter.csproj"

# Build/deployment directories
$PublishDir = Join-Path $RepoRoot "publish"
$BackupDir = Join-Path $RepoRoot "backups"
$LogDir = Join-Path $RepoRoot "logs"
$LogFile = Join-Path $LogDir "deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

# Create log directory
if (-not (Test-Path $LogDir)) {
    New-Item -Path $LogDir -ItemType Directory -Force | Out-Null
}

# ====================================================================
# Helper Functions
# ====================================================================

function Write-Log {
    param(
        [string]$Message, 
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    # Color coding
    switch ($Level) {
        "ERROR"   { Write-Host $logMessage -ForegroundColor Red }
        "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $logMessage -ForegroundColor Green }
        "STEP"    { Write-Host "`n$logMessage" -ForegroundColor Cyan }
        default   { Write-Host $logMessage -ForegroundColor White }
    }
    
    # Write to log file
    Add-Content -Path $LogFile -Value $logMessage
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Stop-IISSafelyWithRetry {
    param(
        [string]$Site,
        [string]$Pool,
        [int]$MaxRetries = 3
    )
    
    try {
        Import-Module WebAdministration -ErrorAction Stop
        
        for ($i = 1; $i -le $MaxRetries; $i++) {
            try {
                # Stop website
                if (Get-Website -Name $Site -ErrorAction SilentlyContinue) {
                    $state = (Get-Website -Name $Site).State
                    if ($state -eq "Started") {
                        Write-Log "Stopping website: $Site (attempt $i/$MaxRetries)"
                        Stop-Website -Name $Site -ErrorAction Stop
                    }
                }
                
                # Stop app pool
                if (Test-Path "IIS:\AppPools\$Pool") {
                    $poolState = (Get-WebAppPoolState -Name $Pool).Value
                    if ($poolState -eq "Started") {
                        Write-Log "Stopping app pool: $Pool (attempt $i/$MaxRetries)"
                        Stop-WebAppPool -Name $Pool -ErrorAction Stop
                    }
                }
                
                # Wait for graceful shutdown
                Start-Sleep -Seconds 3
                
                # Verify stopped
                $websiteStopped = (-not (Get-Website -Name $Site -ErrorAction SilentlyContinue)) -or 
                                  ((Get-Website -Name $Site).State -ne "Started")
                $poolStopped = (-not (Test-Path "IIS:\AppPools\$Pool")) -or 
                               ((Get-WebAppPoolState -Name $Pool).Value -ne "Started")
                
                if ($websiteStopped -and $poolStopped) {
                    Write-Log "IIS stopped successfully" "SUCCESS"
                    return $true
                }
                
                if ($i -lt $MaxRetries) {
                    Write-Log "Retrying..." "WARNING"
                    Start-Sleep -Seconds 2
                }
            }
            catch {
                Write-Log "Stop attempt $i failed: $_" "WARNING"
                if ($i -eq $MaxRetries) {
                    throw
                }
                Start-Sleep -Seconds 2
            }
        }
        
        return $false
    }
    catch {
        Write-Log "Failed to stop IIS after $MaxRetries attempts: $_" "ERROR"
        return $false
    }
}

function Start-IISSafely {
    param([string]$Site, [string]$Pool)
    
    try {
        Import-Module WebAdministration -ErrorAction Stop
        
        # Start app pool first
        if (Test-Path "IIS:\AppPools\$Pool") {
            $poolState = (Get-WebAppPoolState -Name $Pool).Value
            if ($poolState -ne "Started") {
                Write-Log "Starting app pool: $Pool"
                Start-WebAppPool -Name $Pool
                Start-Sleep -Seconds 2
            }
        }
        
        # Start website
        if (Get-Website -Name $Site -ErrorAction SilentlyContinue) {
            $state = (Get-Website -Name $Site).State
            if ($state -ne "Started") {
                Write-Log "Starting website: $Site"
                Start-Website -Name $Site
                Start-Sleep -Seconds 2
            }
        }
        
        return $true
    }
    catch {
        Write-Log "Error starting IIS: $_" "ERROR"
        return $false
    }
}

function Backup-CurrentDeployment {
    param([string]$Source, [string]$BackupRoot)
    
    if (-not (Test-Path $Source)) {
        Write-Log "No existing deployment to backup" "WARNING"
        return $null
    }
    
    try {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $backupPath = Join-Path $BackupRoot "backup-$timestamp"
        
        Write-Log "Creating backup: $backupPath"
        
        if (-not (Test-Path $BackupRoot)) {
            New-Item -Path $BackupRoot -ItemType Directory -Force | Out-Null
        }
        
        # Backup entire deployment
        Copy-Item -Path $Source -Destination $backupPath -Recurse -Force
        
        # Compress old backups (keep last 5 uncompressed)
        $backups = Get-ChildItem -Path $BackupRoot -Directory | 
                   Sort-Object CreationTime -Descending
        
        if ($backups.Count -gt 5) {
            $toCompress = $backups | Select-Object -Skip 5
            foreach ($backup in $toCompress) {
                if (-not (Test-Path "$($backup.FullName).zip")) {
                    Compress-Archive -Path $backup.FullName -DestinationPath "$($backup.FullName).zip" -CompressionLevel Optimal
                    Remove-Item -Path $backup.FullName -Recurse -Force
                    Write-Log "Compressed old backup: $($backup.Name)"
                }
            }
        }
        
        Write-Log "Backup completed: $backupPath" "SUCCESS"
        return $backupPath
    }
    catch {
        Write-Log "Backup failed: $_" "ERROR"
        return $null
    }
}

function Restore-FromBackup {
    param([string]$BackupPath, [string]$DestinationPath)
    
    try {
        Write-Log "Rolling back to: $BackupPath" "WARNING"
        
        if (Test-Path $DestinationPath) {
            Remove-Item -Path $DestinationPath -Recurse -Force
        }
        
        Copy-Item -Path $BackupPath -Destination $DestinationPath -Recurse -Force
        
        Write-Log "Rollback completed" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Rollback failed: $_" "ERROR"
        return $false
    }
}

# ====================================================================
# Main Deployment Process
# ====================================================================

Write-Log "========================================" "STEP"
Write-Log "?? MyStore Supply Co. - Universal Deployment" "STEP"
Write-Log "Environment: $Environment"
Write-Log "Repository Root: $RepoRoot"
Write-Log "Project Directory: $ProjectDir"
Write-Log "Deployment Path: $DeployPath"
Write-Log "========================================" "STEP"

# Check prerequisites
if (-not (Test-Administrator)) {
    Write-Log "? This script requires Administrator privileges!" "ERROR"
    Write-Log "Right-click PowerShell and select 'Run as Administrator'" "ERROR"
    exit 1
}

if (-not (Test-Path $ProjectFile)) {
    Write-Log "? Project file not found: $ProjectFile" "ERROR"
    exit 1
}

# Check if dotnet CLI is available
try {
    $dotnetVersion = dotnet --version
    Write-Log "? .NET SDK version: $dotnetVersion" "SUCCESS"
}
catch {
    Write-Log "? .NET SDK not found! Please install .NET 8 SDK" "ERROR"
    exit 1
}

$backupPath = $null

try {
    # ====================================================================
    # STEP 1: Backup Current Deployment
    # ====================================================================
    
    if (-not $SkipBackup -and (Test-Path $DeployPath)) {
        Write-Log "========================================" "STEP"
        Write-Log "STEP 1: Creating Backup"
        Write-Log "========================================" "STEP"
        
        $backupPath = Backup-CurrentDeployment -Source $DeployPath -BackupRoot $BackupDir
        
        if (-not $backupPath) {
            Write-Log "? Backup failed - continuing anyway (risky!)" "WARNING"
        }
    }
    else {
        Write-Log "Skipping backup (first deployment or -SkipBackup flag)" "WARNING"
    }

    # ====================================================================
    # STEP 2: Build & Publish Application
    # ====================================================================
    
    Write-Log "========================================" "STEP"
    Write-Log "STEP 2: Building Application"
    Write-Log "========================================" "STEP"
    
    # Clean publish directory
    if (Test-Path $PublishDir) {
        Write-Log "Cleaning publish directory..."
        Remove-Item -Path $PublishDir -Recurse -Force
    }
    
    # Restore packages
    Write-Log "Restoring NuGet packages..."
    $restoreOutput = dotnet restore $ProjectFile 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Log "? NuGet restore failed!" "ERROR"
        Write-Log $restoreOutput "ERROR"
        throw "NuGet restore failed"
    }
    Write-Log "? Packages restored" "SUCCESS"
    
    # Build
    Write-Log "Building project (Release configuration)..."
    $buildOutput = dotnet build $ProjectFile --configuration Release --no-restore 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Log "? Build failed!" "ERROR"
        Write-Log $buildOutput "ERROR"
        throw "Build failed"
    }
    Write-Log "? Build successful" "SUCCESS"
    
    # Publish
    Write-Log "Publishing application..."
    $publishOutput = dotnet publish $ProjectFile `
        --configuration Release `
        --output $PublishDir `
        --no-build `
        --runtime win-x64 `
        --self-contained false 2>&1
        
    if ($LASTEXITCODE -ne 0) {
        Write-Log "? Publish failed!" "ERROR"
        Write-Log $publishOutput "ERROR"
        throw "Publish failed"
    }
    Write-Log "? Publish successful" "SUCCESS"

    # ====================================================================
    # STEP 3: Stop IIS
    # ====================================================================
    
    Write-Log "========================================" "STEP"
    Write-Log "STEP 3: Stopping IIS"
    Write-Log "========================================" "STEP"
    
    $stopped = Stop-IISSafelyWithRetry -Site $SiteName -Pool $AppPoolName
    if (-not $stopped) {
        Write-Log "? Could not stop IIS gracefully - may not exist yet" "WARNING"
    }

    # ====================================================================
    # STEP 4: Deploy Files
    # ====================================================================
    
    Write-Log "========================================" "STEP"
    Write-Log "STEP 4: Deploying Files"
    Write-Log "========================================" "STEP"
    
    # Create deployment directory
    if (-not (Test-Path $DeployPath)) {
        Write-Log "Creating deployment directory: $DeployPath"
        New-Item -Path $DeployPath -ItemType Directory -Force | Out-Null
    }
    
    # Preserve configuration files if they exist
    $configFiles = @(
        "appsettings.Production.json",
        "appsettings.$Environment.json"
    )
    
    $preservedConfigs = @{}
    foreach ($configFile in $configFiles) {
        $configPath = Join-Path $DeployPath $configFile
        if (Test-Path $configPath) {
            Write-Log "Preserving config: $configFile"
            $preservedConfigs[$configFile] = Get-Content $configPath -Raw
        }
    }
    
    # Remove old files (except web.config and preserved configs)
    Write-Log "Removing old files..."
    Get-ChildItem -Path $DeployPath -Exclude "web.config","appsettings.*.json","logs" | 
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    
    # Copy new files
    Write-Log "Copying new files..."
    Copy-Item -Path "$PublishDir\*" -Destination $DeployPath -Recurse -Force
    
    # Restore preserved configs
    foreach ($configFile in $preservedConfigs.Keys) {
        $configPath = Join-Path $DeployPath $configFile
        Write-Log "Restoring config: $configFile"
        Set-Content -Path $configPath -Value $preservedConfigs[$configFile]
    }
    
    Write-Log "? Files deployed successfully" "SUCCESS"

    # ====================================================================
    # STEP 5: Configure IIS (if needed)
    # ====================================================================
    
    if (-not $QuickDeploy) {
        Write-Log "========================================" "STEP"
        Write-Log "STEP 5: Configuring IIS"
        Write-Log "========================================" "STEP"
        
        try {
            Import-Module WebAdministration -ErrorAction Stop
            
            # Create/Configure Application Pool
            if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
                Write-Log "Creating application pool: $AppPoolName"
                New-WebAppPool -Name $AppPoolName
            }
            
            Write-Log "Configuring application pool..."
            Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
            Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
            Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
            Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name enable32BitAppOnWin64 -Value $false
            Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.idleTimeout -Value "00:00:00"
            
            # Create/Update Website
            if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
                Write-Log "Updating existing website..."
                Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $DeployPath
                Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
            }
            else {
                Write-Log "Creating new website..."
                New-Website -Name $SiteName `
                    -Port $Port `
                    -PhysicalPath $DeployPath `
                    -ApplicationPool $AppPoolName `
                    -Force
            }
            
            # Configure HTTPS binding
            if ($HttpsPort -ne 0) {
                $httpsBinding = Get-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -ErrorAction SilentlyContinue
                if (-not $httpsBinding) {
                    Write-Log "Adding HTTPS binding (port $HttpsPort)"
                    New-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -IPAddress "*"
                    Write-Log "? HTTPS certificate needs to be configured manually" "WARNING"
                }
            }
            
            # Set directory permissions
            Write-Log "Setting directory permissions..."
            $acl = Get-Acl $DeployPath
            
            $identities = @("IIS_IUSRS", "IUSR")
            foreach ($identity in $identities) {
                $identityRef = New-Object System.Security.Principal.NTAccount($identity)
                $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                    $identityRef,
                    [System.Security.AccessControl.FileSystemRights]::ReadAndExecute,
                    ([System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit),
                    [System.Security.AccessControl.PropagationFlags]::None,
                    [System.Security.AccessControl.AccessControlType]::Allow
                )
                $acl.AddAccessRule($accessRule)
            }
            
            Set-Acl -Path $DeployPath -AclObject $acl
            
            Write-Log "? IIS configured successfully" "SUCCESS"
        }
        catch {
            Write-Log "? IIS configuration failed: $_" "WARNING"
            Write-Log "You may need to configure IIS manually" "WARNING"
        }
    }
    else {
        Write-Log "Skipping IIS configuration (QuickDeploy mode)" "WARNING"
    }

    # ====================================================================
    # STEP 6: Database Migrations
    # ====================================================================
    
    if (-not $SkipDatabase) {
        Write-Log "========================================" "STEP"
        Write-Log "STEP 6: Applying Database Migrations"
        Write-Log "========================================" "STEP"
        
        try {
            Push-Location $ProjectDir
            
            # Check if EF tools are available
            $efCheck = dotnet ef --version 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Log "? EF Core tools not installed" "WARNING"
                Write-Log "Installing EF Core tools..." "WARNING"
                dotnet tool install --global dotnet-ef
            }
            
            Write-Log "Applying migrations..."
            $migrationOutput = dotnet ef database update --project $ProjectFile 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Log "? Database migrations completed" "SUCCESS"
            }
            else {
                Write-Log "? Migration warning: $migrationOutput" "WARNING"
                Write-Log "Database may need manual migration" "WARNING"
            }
            
            Pop-Location
        }
        catch {
            Write-Log "? Database migration error: $_" "WARNING"
            Pop-Location
        }
    }
    else {
        Write-Log "Skipping database migrations" "WARNING"
    }

    # ====================================================================
    # STEP 7: Start IIS
    # ====================================================================
    
    Write-Log "========================================" "STEP"
    Write-Log "STEP 7: Starting IIS"
    Write-Log "========================================" "STEP"
    
    Start-Sleep -Seconds 2
    
    $started = Start-IISSafely -Site $SiteName -Pool $AppPoolName
    if ($started) {
        Write-Log "? IIS started successfully" "SUCCESS"
    }
    else {
        Write-Log "? Failed to start IIS" "ERROR"
        throw "IIS start failed"
    }

    # ====================================================================
    # STEP 8: Verification
    # ====================================================================
    
    Write-Log "========================================" "STEP"
    Write-Log "STEP 8: Verifying Deployment"
    Write-Log "========================================" "STEP"
    
    Start-Sleep -Seconds 3
    
    # Check IIS status
    try {
        $poolState = (Get-WebAppPoolState -Name $AppPoolName).Value
        $siteState = (Get-WebsiteState -Name $SiteName).Value
        
        if ($poolState -eq "Started") {
            Write-Log "? App pool is running" "SUCCESS"
        }
        else {
            Write-Log "? App pool not running: $poolState" "ERROR"
        }
        
        if ($siteState -eq "Started") {
            Write-Log "? Website is running" "SUCCESS"
        }
        else {
            Write-Log "? Website not running: $siteState" "ERROR"
        }
    }
    catch {
        Write-Log "? Could not verify IIS status" "WARNING"
    }
    
    # Check critical files
    $criticalFiles = @(
        "EcommerceStarter.dll",
        "appsettings.json",
        "web.config"
    )
    
    $allFilesExist = $true
    foreach ($file in $criticalFiles) {
        $filePath = Join-Path $DeployPath $file
        if (Test-Path $filePath) {
            Write-Log "? $file" "SUCCESS"
        }
        else {
            Write-Log "? $file missing!" "ERROR"
            $allFilesExist = $false
        }
    }
    
    if (-not $allFilesExist) {
        throw "Critical files missing!"
    }
    
    # Try to make a test request
    try {
        Write-Log "Testing website response..."
        $response = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Log "? Website responding (HTTP 200)" "SUCCESS"
        }
        else {
            Write-Log "? Website returned status: $($response.StatusCode)" "WARNING"
        }
    }
    catch {
        Write-Log "? Could not verify website response: $_" "WARNING"
        Write-Log "Check IIS logs and application logs for errors" "WARNING"
    }

    # ====================================================================
    # Deployment Success
    # ====================================================================
    
    Write-Log "========================================" "STEP"
    Write-Log "? DEPLOYMENT SUCCESSFUL!" "SUCCESS"
    Write-Log "========================================" "STEP"
    
}
catch {
    Write-Log "========================================" "ERROR"
    Write-Log "? DEPLOYMENT FAILED!" "ERROR"
    Write-Log "Error: $_" "ERROR"
    Write-Log "========================================" "ERROR"
    
    # Attempt rollback
    if ($backupPath -and (Test-Path $backupPath)) {
        Write-Log ""
        Write-Log "Attempting automatic rollback..." "WARNING"
        
        Stop-IISSafelyWithRetry -Site $SiteName -Pool $AppPoolName | Out-Null
        $rollbackSuccess = Restore-FromBackup -BackupPath $backupPath -DestinationPath $DeployPath
        
        if ($rollbackSuccess) {
            Start-IISSafely -Site $SiteName -Pool $AppPoolName | Out-Null
            Write-Log "? Rollback completed - previous version restored" "SUCCESS"
        }
        else {
            Write-Log "? Rollback failed! Manual intervention required!" "ERROR"
        }
    }
    
    exit 1
}

# ====================================================================
# Summary & Next Steps
# ====================================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "?? Deployment Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environment:        $Environment" -ForegroundColor White
Write-Host "Deployment Path:    $DeployPath" -ForegroundColor White
Write-Host "Website:            $SiteName" -ForegroundColor White
Write-Host "App Pool:           $AppPoolName" -ForegroundColor White
Write-Host "Backup Location:    $backupPath" -ForegroundColor White
Write-Host "Log File:           $LogFile" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "?? Access Your Site" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "HTTP:   http://localhost:$Port" -ForegroundColor Green
if ($HttpsPort -ne 0) {
    Write-Host "HTTPS:  https://localhost:$HttpsPort" -ForegroundColor Green
}
Write-Host ""
Write-Host "Admin:  http://localhost:$Port/Admin/Dashboard" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "? Next Steps" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Test the website in your browser" -ForegroundColor White
Write-Host "2. Check application logs for any errors" -ForegroundColor White
Write-Host "3. Verify database migrations applied correctly" -ForegroundColor White
Write-Host "4. Test key functionality (login, checkout, etc.)" -ForegroundColor White
Write-Host ""

exit 0

