<#
.SYNOPSIS
    Build a portable, all-inclusive installer package for EcommerceStarter
    
.DESCRIPTION
    This script creates a complete, self-contained installer package that includes:
    - Pre-built application binaries (Release mode)
    - EF Core migrations bundle (standalone executable)
    - Installer executable
    - All dependencies bundled
    
    The resulting package can be distributed to any Windows Server machine with:
    - IIS installed
    - SQL Server installed
    - .NET 8 Runtime (ASP.NET Core) installed
    
.PARAMETER OutputPath
    Directory where the final package will be created (default: .\Packages)
    
.PARAMETER Version
    Version number for the package (default: 1.0.0)
    
.EXAMPLE
    .\Build-PortableInstaller.ps1
    
.EXAMPLE
    .\Build-PortableInstaller.ps1 -OutputPath "C:\Releases" -Version "1.2.3"
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\Packages",
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

# ============================================================================
# Configuration
# ============================================================================

$ScriptRoot = $PSScriptRoot
$ProjectRoot = $ScriptRoot
$MainProject = Join-Path $ProjectRoot "EcommerceStarter\EcommerceStarter.csproj"
$InstallerProject = Join-Path $ProjectRoot "EcommerceStarter.Installer\EcommerceStarter.Installer.csproj"

$TempBuildDir = Join-Path $ProjectRoot "temp_build"
$AppPublishDir = Join-Path $TempBuildDir "app"
$InstallerBuildDir = Join-Path $TempBuildDir "installer"
$MigrationBundleDir = Join-Path $TempBuildDir "migrations"

$PackageName = "EcommerceStarter-Installer-v$Version"
$FinalPackageDir = Join-Path $OutputPath $PackageName

# ============================================================================
# Helper Functions
# ============================================================================

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "??  $Message" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Red
}

# ============================================================================
# Pre-flight Checks
# ============================================================================

Write-Step "Pre-flight Checks"

# Check if projects exist
if (-not (Test-Path $MainProject)) {
    Write-Error-Custom "Main project not found: $MainProject"
    exit 1
}

if (-not (Test-Path $InstallerProject)) {
    Write-Error-Custom "Installer project not found: $InstallerProject"
    exit 1
}

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK version: $dotnetVersion"
} catch {
    Write-Error-Custom ".NET SDK not found! Please install .NET 8 SDK"
    exit 1
}

# Check if dotnet-ef tool is installed
try {
    dotnet ef --version | Out-Null
    Write-Success "EF Core tools are installed"
} catch {
    Write-Info "Installing EF Core tools..."
    dotnet tool install --global dotnet-ef
    Write-Success "EF Core tools installed"
}

# ============================================================================
# Clean Previous Build
# ============================================================================

Write-Step "Cleaning Previous Build"

if (Test-Path $TempBuildDir) {
    Write-Info "Removing old temp build directory..."
    Remove-Item -Path $TempBuildDir -Recurse -Force
}

if (Test-Path $FinalPackageDir) {
    Write-Info "Removing old package directory..."
    Remove-Item -Path $FinalPackageDir -Recurse -Force
}

# Create directories
New-Item -ItemType Directory -Path $TempBuildDir -Force | Out-Null
New-Item -ItemType Directory -Path $AppPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $InstallerBuildDir -Force | Out-Null
New-Item -ItemType Directory -Path $MigrationBundleDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
New-Item -ItemType Directory -Path $FinalPackageDir -Force | Out-Null

Write-Success "Build directories created"

# ============================================================================
# Step 1: Publish Main Application
# ============================================================================

Write-Step "Step 1: Publishing Main Application (Release Mode)"

Write-Info "Restoring NuGet packages..."
dotnet restore $MainProject

Write-Info "Publishing application with production optimizations..."
dotnet publish $MainProject `
    --configuration Release `
    --output $AppPublishDir `
    --runtime win-x64 `
    --no-self-contained `
    /p:PublishReadyToRun=true `
    /p:EnvironmentName=Production

if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Application publish failed!"
    exit 1
}

Write-Success "Application published to: $AppPublishDir"

# Remove development files
$devFiles = @(
    "appsettings.Development.json",
    "appsettings.Staging.json"
)

foreach ($file in $devFiles) {
    $filePath = Join-Path $AppPublishDir $file
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
        Write-Info "Removed development file: $file"
    }
}

# ============================================================================
# Step 2: Create EF Migrations Bundle
# ============================================================================

Write-Step "Step 2: Creating EF Migrations Bundle"

Write-Info "Creating standalone migration executable..."

Push-Location (Split-Path $MainProject -Parent)

dotnet ef migrations bundle `
    --project $MainProject `
    --context ApplicationDbContext `
    --configuration Release `
    --runtime win-x64 `
    --output (Join-Path $MigrationBundleDir "efbundle.exe") `
    --force

Pop-Location

if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Migration bundle creation failed!"
    exit 1
}

Write-Success "Migration bundle created: efbundle.exe"

# ============================================================================
# Step 3: Build Installer
# ============================================================================

Write-Step "Step 3: Building Installer"

Write-Info "Building installer in Release mode..."

dotnet build $InstallerProject `
    --configuration Release `
    --output $InstallerBuildDir

if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Installer build failed!"
    exit 1
}

Write-Success "Installer built successfully"

# ============================================================================
# Step 4: Package Everything Together
# ============================================================================

Write-Step "Step 4: Creating Final Package"

Write-Info "Copying files to package directory..."

# Create package structure
$packageAppDir = Join-Path $FinalPackageDir "app"
$packageMigrationDir = Join-Path $FinalPackageDir "migrations"

New-Item -ItemType Directory -Path $packageAppDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageMigrationDir -Force | Out-Null

# Copy application files
Write-Info "Copying application files..."
Copy-Item -Path "$AppPublishDir\*" -Destination $packageAppDir -Recurse -Force

# Copy migration bundle
Write-Info "Copying migration bundle..."
Copy-Item -Path "$MigrationBundleDir\*" -Destination $packageMigrationDir -Recurse -Force

# Copy installer files
Write-Info "Copying installer files..."
Copy-Item -Path "$InstallerBuildDir\*" -Destination $FinalPackageDir -Recurse -Force

# Create README
$readmeContent = @"
# EcommerceStarter Installer v$Version

## ?? Package Contents

This package contains everything needed to deploy EcommerceStarter to a production Windows Server.

### Files Included:
- **EcommerceStarter.Installer.exe** - Main installer application (run this)
- **app/** - Pre-built application files (Release mode, optimized)
- **migrations/** - Standalone EF Core migration executable

## ?? Prerequisites on Target Server

Before running the installer, ensure the target server has:

1. ? **Windows Server 2016 or later** (or Windows 10/11)
2. ? **IIS installed** with:
   - ASP.NET Core Module v2
   - WebAdministration PowerShell module
3. ? **SQL Server** (Express, Standard, or Enterprise)
   - SQL Browser service (if using named instance)
4. ? **.NET 8 Runtime** (ASP.NET Core Runtime)
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0

### Installing Prerequisites

Run these PowerShell commands as Administrator:

``````powershell
# Install IIS
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

# Install ASP.NET Core Module (required)
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Look for "ASP.NET Core Runtime 8.x Hosting Bundle"
``````

## ?? Installation Instructions

### Quick Install:

1. **Extract this package** to any folder on the target server
2. **Right-click** on ``EcommerceStarter.Installer.exe``
3. Select **"Run as Administrator"**
4. Follow the installation wizard

### What the Installer Does:

1. ? Deploys application files to ``C:\inetpub\wwwroot\[YourSiteName]``
2. ? Creates and configures database (runs migrations)
3. ? Creates IIS application pool
4. ? Configures IIS website
5. ? Creates admin user account
6. ? Registers in Windows Programs & Features

## ?? Post-Installation

After installation completes:

1. **Access your site**: ``http://localhost/[YourSiteName]``
2. **Login to admin panel**: Use the email/password you provided
3. **Configure additional settings** in the admin panel

## ?? Troubleshooting

### Installer won't start
- Ensure you're running as Administrator
- Check that .NET 8 Runtime is installed

### Database creation fails
- Verify SQL Server is running
- For named instances, ensure SQL Browser service is running
- Check Windows Firewall isn't blocking SQL Server

### Website shows 500 error
- Check ``C:\inetpub\wwwroot\[YourSiteName]\logs\`` for error details
- Verify IIS Application Pool is running
- Ensure database connection string is correct

### Need to reconfigure?
- Run the installer again - it will detect existing installation
- You can change settings without losing data

## ?? Support

For issues or questions:
- Check logs in ``C:\inetpub\wwwroot\[YourSiteName]\logs\``
- Review IIS logs in ``C:\inetpub\logs\LogFiles\``
- Check Event Viewer ? Windows Logs ? Application

---

**Built:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Version:** $Version
"@

$readmeContent | Out-File -FilePath (Join-Path $FinalPackageDir "README.txt") -Encoding UTF8

Write-Success "README.txt created"

# ============================================================================
# Step 5: Create ZIP Archive
# ============================================================================

Write-Step "Step 5: Creating ZIP Archive"

$zipPath = Join-Path $OutputPath "$PackageName.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Write-Info "Compressing package..."
Compress-Archive -Path "$FinalPackageDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zipSize = (Get-Item $zipPath).Length / 1MB
Write-Success "ZIP archive created: $zipPath ($([math]::Round($zipSize, 2)) MB)"

# ============================================================================
# Cleanup
# ============================================================================

Write-Step "Cleanup"

Write-Info "Removing temporary build directory..."
Remove-Item -Path $TempBuildDir -Recurse -Force
Write-Success "Cleanup complete"

# ============================================================================
# Summary
# ============================================================================

Write-Step "? Build Complete!"

Write-Host ""
Write-Host "?? Package Location:" -ForegroundColor Cyan
Write-Host "   Folder: $FinalPackageDir" -ForegroundColor White
Write-Host "   ZIP:    $zipPath" -ForegroundColor White
Write-Host ""
Write-Host "?? Package Contents:" -ForegroundColor Cyan
Write-Host "   • Installer:       EcommerceStarter.Installer.exe" -ForegroundColor White
Write-Host "   • Application:     app\ folder" -ForegroundColor White
Write-Host "   • Migrations:      migrations\efbundle.exe" -ForegroundColor White
Write-Host "   • Documentation:   README.txt" -ForegroundColor White
Write-Host ""
Write-Host "?? Distribution:" -ForegroundColor Cyan
Write-Host "   1. Copy the ZIP file to target server" -ForegroundColor White
Write-Host "   2. Extract anywhere" -ForegroundColor White
Write-Host "   3. Run EcommerceStarter.Installer.exe as Administrator" -ForegroundColor White
Write-Host ""
Write-Host "? No source code needed on target server!" -ForegroundColor Green
Write-Host "? No .NET SDK needed on target server!" -ForegroundColor Green
Write-Host "? Only requires .NET 8 Runtime + IIS + SQL Server" -ForegroundColor Green
Write-Host ""
