#!/usr/bin/env powershell
<#
.SYNOPSIS
    Create a standalone distributable installer package for EcommerceStarter
    
.DESCRIPTION
    This script creates a single .zip file containing everything needed for deployment:
    - The installer executable
    - The published web application
    - Documentation and deployment guides
    
    The resulting package can be distributed to customers or deployment teams.

.PARAMETER OutputFile
    The output filename for the distributable package (default: EcommerceStarter-Production-v1.0.0.zip)

.PARAMETER Version
    Version number (default: 1.0.0)

.PARAMETER IncludeSourceCode
    If $true, includes source code (default: $false for production)

.EXAMPLE
    .\Create-DistributablePackage.ps1 -OutputFile "EcommerceStarter-v2.0.0.zip" -Version "2.0.0"
#>

param(
    [string]$OutputFile = "EcommerceStarter-Production-v1.0.0.zip",
    [string]$Version = "1.0.0",
    [bool]$IncludeSourceCode = $false
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$colors = @{
    Success = "Green"
    Error   = "Red"
    Warning = "Yellow"
    Info    = "Cyan"
}

function Write-Log {
    param([string]$Message, [ValidateSet("Info", "Success", "Warning", "Error")][string]$Level = "Info")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = $colors[$Level]
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-Prerequisites {
    Write-Log "Checking prerequisites for packaging..." -Level Info
    
    # Check if build artifacts exist
    $buildPath = ".\dist"
    if (-not (Test-Path $buildPath)) {
        Write-Log "Build artifacts not found at $buildPath" -Level Error
        Write-Log "Please run Build-ProductionPackage.ps1 first" -Level Warning
        exit 1
    }
    
    $appPath = Join-Path -Path $buildPath -ChildPath "Application"
    $installerPath = Join-Path -Path $buildPath -ChildPath "Installer"
    
    if (-not (Test-Path $appPath)) {
        Write-Log "Application files not found at $appPath" -Level Error
        exit 1
    }
    
    if (-not (Test-Path $installerPath)) {
        Write-Log "Installer files not found at $installerPath" -Level Error
        exit 1
    }
    
    Write-Log "Build artifacts verified" -Level Success
}

function New-Package {
    Write-Log "Creating distributable package..." -Level Info
    
    $tempDir = Join-Path -Path $env:TEMP -ChildPath "EcommerceBuild_$([guid]::NewGuid().ToString().Substring(0,8))"
    $packageDir = Join-Path -Path $tempDir -ChildPath "EcommerceStarter-v$Version"
    
    Write-Log "Staging package in: $packageDir" -Level Info
    New-Item -Path $packageDir -ItemType Directory -Force | Out-Null
    
    # Copy installer
    $installerSrc = ".\dist\Installer"
    $installerDst = Join-Path -Path $packageDir -ChildPath "Installer"
    Copy-Item -Path $installerSrc -Destination $installerDst -Recurse -Force
    Write-Log "Copied installer" -Level Success
    
    # Copy application
    $appSrc = ".\dist\Application"
    $appDst = Join-Path -Path $packageDir -ChildPath "Application"
    Copy-Item -Path $appSrc -Destination $appDst -Recurse -Force
    Write-Log "Copied application files" -Level Success
    
    # Copy documentation
    if (Test-Path ".\dist\PRODUCTION_DEPLOYMENT_README.md") {
        Copy-Item -Path ".\dist\PRODUCTION_DEPLOYMENT_README.md" `
                  -Destination (Join-Path -Path $packageDir -ChildPath "README.md") -Force
    }
    
    if (Test-Path ".\dist\BUILD_REPORT.txt") {
        Copy-Item -Path ".\dist\BUILD_REPORT.txt" `
                  -Destination (Join-Path -Path $packageDir -ChildPath "BUILD_REPORT.txt") -Force
    }
    
    # Create quick start guide
    New-Item -Path $packageDir -Name "QUICK_START.txt" -ItemType File -Force | Out-Null
    $quickStart = @"
ECOMMERCESTARTER - QUICK START GUIDE
================================================================================

STEP 1: EXTRACT PACKAGE
- Extract this ZIP file to a temporary location
- Keep the folder structure intact

STEP 2: RUN INSTALLER
1. Navigate to the "Installer" folder
2. Right-click "EcommerceStarter.Installer.exe"
3. Select "Run as administrator"
4. Follow the installation wizard

STEP 3: INITIAL SETUP
1. Open your browser and navigate to: http://localhost/EcommerceStarter
2. Log in with the admin credentials you created during installation
3. Complete the setup wizard
4. Configure your store settings (branding, shipping, payment)

STEP 4: PRODUCTION DEPLOYMENT (Optional)
1. Deploy to your production server using the included application files
2. Configure HTTPS/SSL at your hosting provider
3. Update DNS records to point to your server
4. Monitor application logs and performance

REQUIREMENTS
================================================================================
- Windows Server 2016 or later (or Windows 10/11 with IIS)
- .NET 8 Runtime or Hosting Bundle
- SQL Server 2016 or later
- IIS 10.0 or later
- Administrator access to the server

SUPPORT
================================================================================
For issues or questions:
- GitHub: https://github.com/davidtres03/EcommerceStarter
- Documentation: See PRODUCTION_DEPLOYMENT_README.md

Version: $Version
Build Date: $(Get-Date -Format 'yyyy-MM-dd')
"@
    Set-Content -Path (Join-Path -Path $packageDir -ChildPath "QUICK_START.txt") -Value $quickStart -Encoding UTF8
    Write-Log "Created quick start guide" -Level Info
    
    # Create release notes
    $releaseNotes = @"
ECOMMERCESTARTER v$Version RELEASE NOTES
================================================================================

This is a production-ready release of EcommerceStarter.

WHAT'S INCLUDED
================================================================================
- Windows installer (EcommerceStarter.Installer.exe)
- Published web application (Release build)
- Complete documentation
- Production configuration

PRODUCTION FEATURES
================================================================================
? Optimized Release build
? ReadyToRun compilation enabled
? No debug symbols or information
? Security headers configured
? HTTP compression enabled
? Static content caching configured
? Production logging configured
? Database setup automation

INSTALLATION
================================================================================
See QUICK_START.txt and README.md for detailed instructions.

SYSTEM REQUIREMENTS
================================================================================
- OS: Windows Server 2016+ or Windows 10/11 with IIS
- Runtime: .NET 8 Runtime (or Hosting Bundle)
- Database: SQL Server 2016+
- Web Server: IIS 10.0+
- RAM: Minimum 2GB (recommended 4GB+)
- Storage: Minimum 2GB for installation

BREAKING CHANGES
================================================================================
None for this release.

KNOWN ISSUES
================================================================================
None identified.

UPGRADE INSTRUCTIONS
================================================================================
1. Backup your database
2. Backup your current installation
3. Run the new installer
4. Select upgrade when prompted
5. Review configuration
6. Complete installation

For more details, see PRODUCTION_DEPLOYMENT_README.md

================================================================================
"@
    Set-Content -Path (Join-Path -Path $packageDir -ChildPath "RELEASE_NOTES.txt") -Value $releaseNotes -Encoding UTF8
    Write-Log "Created release notes" -Level Info
    
    # Create the ZIP file
    Write-Log "Creating ZIP package..." -Level Info
    
    $OutputFile = $OutputFile -replace '.zip$', '' # Remove .zip if provided
    $OutputFile = "$OutputFile.zip"
    
    # Remove existing package if it exists
    if (Test-Path $OutputFile) {
        Remove-Item -Path $OutputFile -Force
        Write-Log "Removed existing package file" -Level Info
    }
    
    # Use PowerShell 5.0+ Compress-Archive
    Compress-Archive -Path $packageDir -DestinationPath $OutputFile -CompressionLevel Optimal -Force
    
    if (-not (Test-Path $OutputFile)) {
        Write-Log "Failed to create ZIP package" -Level Error
        exit 1
    }
    
    # Get file size
    $fileSize = (Get-Item $OutputFile).Length / 1MB
    Write-Log "ZIP package created: $OutputFile ($([math]::Round($fileSize, 2)) MB)" -Level Success
    
    # Cleanup temp directory
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    
    return $OutputFile
}

function Show-PackageInfo {
    param([string]$PackageFile)
    
    Write-Log "" -Level Info
    Write-Log "========================================" -Level Success
    Write-Log "Package created successfully!" -Level Success
    Write-Log "========================================" -Level Success
    Write-Log "" -Level Info
    Write-Log "Package File: $PackageFile" -Level Info
    Write-Log "Version: $Version" -Level Info
    Write-Log "File Size: $([math]::Round((Get-Item $PackageFile).Length / 1MB, 2)) MB" -Level Info
    Write-Log "" -Level Info
    Write-Log "Distribution Instructions:" -Level Info
    Write-Log "1. Share this ZIP file with your customer or deployment team" -Level Info
    Write-Log "2. They should extract the ZIP file on their server" -Level Info
    Write-Log "3. They should run: Installer\EcommerceStarter.Installer.exe" -Level Info
    Write-Log "4. Follow the installation wizard" -Level Info
    Write-Log "" -Level Info
}

# Main execution
function Main {
    Write-Log "========================================" -Level Info
    Write-Log "Creating Distributable Package" -Level Info
    Write-Log "========================================" -Level Info
    Write-Log "Version: $Version" -Level Info
    Write-Log "Output: $OutputFile" -Level Info
    Write-Log "" -Level Info
    
    try {
        Test-Prerequisites
        $packageFile = New-Package
        Show-PackageInfo -PackageFile $packageFile
    }
    catch {
        Write-Log $_.Exception.Message -Level Error
        exit 1
    }
}

Main
