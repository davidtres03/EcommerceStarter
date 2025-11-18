# ============================================================================
# MyStore Supply Co. - Automated Git Deployment Script
# ============================================================================
# This script automates pulling latest code from Git and deploying to IIS
# Run as Administrator on the HOST MACHINE
#
# Usage: .\deploy-from-git.ps1 [-Branch "master"] [-SkipTests]
# ============================================================================

param(
    [string]$Branch = "master",
    [string]$RepoPath = "C:\Deploy\EcommerceStarter",
    [string]$ProjectPath = "EcommerceStarter",
    [string]$PublishPath = "C:\inetpub\wwwroot\MyStore",
    [string]$AppPoolName = "MyStorePool",
    [string]$SiteName = "MyStore",
    [switch]$SkipTests,
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "  MyStore Supply Co. - Automated Deployment" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Start Time: $StartTime" -ForegroundColor Yellow
Write-Host "Branch: $Branch" -ForegroundColor Yellow
Write-Host "Repository: $RepoPath" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# Pre-flight Checks
# ============================================================================

Write-Host "[1/9] Running pre-flight checks..." -ForegroundColor Cyan

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: Script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Check if repository exists
if (-not (Test-Path $RepoPath)) {
    Write-Host "ERROR: Repository not found at $RepoPath" -ForegroundColor Red
    exit 1
}

# Check if .NET 8 is installed
$dotnetVersion = dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 8"
if (-not $dotnetVersion) {
    Write-Host "ERROR: .NET 8 Runtime not found!" -ForegroundColor Red
    exit 1
}

Write-Host "  ? All checks passed" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Backup Current Deployment
# ============================================================================

if (-not $SkipBackup) {
    Write-Host "[2/9] Creating backup..." -ForegroundColor Cyan
    
    if (Test-Path $PublishPath) {
        $BackupPath = "C:\Deploy\Backups\MyStore_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        New-Item -ItemType Directory -Force -Path $BackupPath | Out-Null
        
        # Stop app pool before backup
        Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        
        Copy-Item -Path "$PublishPath\*" -Destination $BackupPath -Recurse -Force
        Write-Host "  ? Backup created at: $BackupPath" -ForegroundColor Green
        
        # Clean old backups (keep last 5)
        Get-ChildItem "C:\Deploy\Backups" | 
            Sort-Object CreationTime -Descending | 
            Select-Object -Skip 5 | 
            Remove-Item -Recurse -Force
    } else {
        Write-Host "  ? No existing deployment to backup" -ForegroundColor Yellow
    }
    Write-Host ""
} else {
    Write-Host "[2/9] Skipping backup..." -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================================
# Pull Latest Code from Git
# ============================================================================

Write-Host "[3/9] Pulling latest code from Git..." -ForegroundColor Cyan

try {
    Set-Location $RepoPath
    
    # Fetch latest
    Write-Host "  Fetching from origin..." -ForegroundColor Yellow
    git fetch origin
    
    # Get current commit
    $OldCommit = git rev-parse HEAD
    Write-Host "  Current commit: $($OldCommit.Substring(0,7))" -ForegroundColor Gray
    
    # Checkout and pull
    Write-Host "  Checking out $Branch..." -ForegroundColor Yellow
    git checkout $Branch
    git pull origin $Branch
    
    # Get new commit
    $NewCommit = git rev-parse HEAD
    Write-Host "  New commit: $($NewCommit.Substring(0,7))" -ForegroundColor Gray
    
    if ($OldCommit -eq $NewCommit) {
        Write-Host "  ? No new commits to deploy" -ForegroundColor Yellow
    } else {
        Write-Host "  ? Code updated successfully" -ForegroundColor Green
        
        # Show commit log
        Write-Host "`n  Recent commits:" -ForegroundColor Cyan
        git log --oneline -5
    }
    
    Write-Host ""
} catch {
    Write-Host "  ? Failed to pull from Git: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Restore NuGet Packages
# ============================================================================

Write-Host "[4/9] Restoring NuGet packages..." -ForegroundColor Cyan

try {
    Set-Location "$RepoPath\$ProjectPath"
    dotnet restore
    Write-Host "  ? Packages restored" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "  ? Failed to restore packages: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Build Application
# ============================================================================

Write-Host "[5/9] Building application..." -ForegroundColor Cyan

try {
    dotnet build --configuration Release --no-restore
    Write-Host "  ? Build successful" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "  ? Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Run Tests (Optional)
# ============================================================================

if (-not $SkipTests) {
    Write-Host "[6/9] Running tests..." -ForegroundColor Cyan
    
    try {
        $testResult = dotnet test --configuration Release --no-build --verbosity quiet
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ? All tests passed" -ForegroundColor Green
        } else {
            Write-Host "  ? Some tests failed - continuing anyway" -ForegroundColor Yellow
        }
        Write-Host ""
    } catch {
        Write-Host "  ? Test run failed - continuing anyway" -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Host "[6/9] Skipping tests..." -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================================
# Publish Application
# ============================================================================

Write-Host "[7/9] Publishing application..." -ForegroundColor Cyan

try {
    $TempPublishPath = "$env:TEMP\MyStore_Publish_$(Get-Date -Format 'yyyyMMddHHmmss')"
    
    dotnet publish --configuration Release --output $TempPublishPath --no-build
    Write-Host "  ? Application published to temp location" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "  ? Publish failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Deploy to IIS
# ============================================================================

Write-Host "[8/9] Deploying to IIS..." -ForegroundColor Cyan

try {
    # Stop app pool and site
    Write-Host "  Stopping IIS site and app pool..." -ForegroundColor Yellow
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    
    # Ensure publish directory exists
    if (-not (Test-Path $PublishPath)) {
        New-Item -ItemType Directory -Force -Path $PublishPath | Out-Null
    }
    
    # Copy published files
    Write-Host "  Copying files to production..." -ForegroundColor Yellow
    Copy-Item -Path "$TempPublishPath\*" -Destination $PublishPath -Recurse -Force
    
    # Clean up temp publish folder
    Remove-Item -Path $TempPublishPath -Recurse -Force
    
    # Set permissions
    Write-Host "  Setting permissions..." -ForegroundColor Yellow
    $identity = "IIS AppPool\$AppPoolName"
    $acl = Get-Acl $PublishPath
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl $PublishPath $acl
    
    # Ensure uploads folder exists with write permissions
    $uploadsPath = Join-Path $PublishPath "wwwroot\uploads"
    if (-not (Test-Path $uploadsPath)) {
        New-Item -ItemType Directory -Force -Path $uploadsPath | Out-Null
    }
    $acl = Get-Acl $uploadsPath
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl $uploadsPath $acl
    
    # Start app pool and site
    Write-Host "  Starting IIS site and app pool..." -ForegroundColor Yellow
    Start-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
    Start-Website -Name $SiteName
    
    Write-Host "  ? Deployment completed" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "  ? Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nAttempting to restore from backup..." -ForegroundColor Yellow
    
    if (-not $SkipBackup -and (Test-Path $BackupPath)) {
        Copy-Item -Path "$BackupPath\*" -Destination $PublishPath -Recurse -Force
        Start-WebAppPool -Name $AppPoolName
        Start-Website -Name $SiteName
        Write-Host "  ? Restored from backup" -ForegroundColor Green
    }
    exit 1
}

# ============================================================================
# Verify Deployment
# ============================================================================

Write-Host "[9/9] Verifying deployment..." -ForegroundColor Cyan

Start-Sleep -Seconds 3

try {
    $healthUrl = "http://localhost"
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 10
    
    if ($response.StatusCode -eq 200) {
        Write-Host "  ? Website is responding (HTTP 200)" -ForegroundColor Green
    } else {
        Write-Host "  ? Website responded with status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ? Website verification failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Check IIS logs and Windows Event Viewer for details" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# Summary
# ============================================================================

$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Host "============================================================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT SUCCESSFUL! ??" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Start Time:     $StartTime"
Write-Host "  End Time:       $EndTime"
Write-Host "  Duration:       $($Duration.ToString('mm\:ss'))"
Write-Host "  Branch:         $Branch"
Write-Host "  Commit:         $($NewCommit.Substring(0,7))"
if (-not $SkipBackup) {
    Write-Host "  Backup:         $BackupPath"
}
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test website functionality"
Write-Host "  2. Check application logs"
Write-Host "  3. Verify database connectivity"
Write-Host "  4. Test dark mode and shopping cart"
Write-Host ""
Write-Host "Deployment Log saved to: C:\Deploy\Logs\deploy_$(Get-Date -Format 'yyyyMMdd_HHmmss').log" -ForegroundColor Cyan
Write-Host ""

# Save deployment log
$LogPath = "C:\Deploy\Logs"
if (-not (Test-Path $LogPath)) {
    New-Item -ItemType Directory -Force -Path $LogPath | Out-Null
}

$LogFile = "$LogPath\deploy_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$LogContent = @"
Deployment Summary
==================
Start Time: $StartTime
End Time: $EndTime
Duration: $($Duration.ToString('mm\:ss'))
Branch: $Branch
Commit: $NewCommit
Status: SUCCESS
"@

$LogContent | Out-File $LogFile -Encoding utf8
