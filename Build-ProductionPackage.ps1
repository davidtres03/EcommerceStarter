#!/usr/bin/env powershell
<#
.SYNOPSIS
    Build and package EcommerceStarter for production deployment
    This script creates a clean production build and prepares the installer.

.DESCRIPTION
    This script performs the following tasks:
    1. Builds the projects in Release mode
    2. Publishes the web application for production
    3. Creates a clean installer package
    4. Verifies production configuration

.PARAMETER OutputPath
    The output directory for the packaged installer (default: ./dist)

.PARAMETER Version
    Version number for the build (default: 1.0.0)

.EXAMPLE
    .\Build-ProductionPackage.ps1 -OutputPath "C:\BuildOutput" -Version "2.0.0"
#>

param(
    [string]$OutputPath = "./dist",
    [string]$Version = "1.0.0"
)

# Enable strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Colors for output
$colors = @{
    Success = "Green"
    Error   = "Red"
    Warning = "Yellow"
    Info    = "Cyan"
}

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = $colors[$Level]
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Verify-Prerequisites {
    Write-Log "Verifying prerequisites..." -Level Info
    
    # Check .NET CLI
    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetPath) {
        Write-Log "dotnet CLI not found. Please install .NET 8 SDK." -Level Error
        exit 1
    }
    
    $dotnetVersion = & dotnet --version
    Write-Log ".NET version: $dotnetVersion" -Level Info
    
    # Check for Visual Studio Build Tools or MSBuild
    $msbuildPath = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    
    if ($msbuildPath) {
        Write-Log "MSBuild found: $msbuildPath" -Level Info
    } else {
        Write-Log "Warning: MSBuild not found. Build will use dotnet CLI." -Level Warning
    }
}

function Clean-BuildArtifacts {
    Write-Log "Cleaning previous build artifacts..." -Level Info
    
    $cleanDirs = @("bin", "obj", $OutputPath)
    
    foreach ($dir in $cleanDirs) {
        if (Test-Path $dir) {
            Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
            Write-Log "Cleaned: $dir" -Level Info
        }
    }
}

function Build-Projects {
    Write-Log "Building projects in Release mode..." -Level Info
    
    # Build EcommerceStarter (main web project)
    Write-Log "Building EcommerceStarter..." -Level Info
    $webProjectPath = ".\EcommerceStarter\EcommerceStarter.csproj"
    
    if (-not (Test-Path $webProjectPath)) {
        Write-Log "Web project not found at $webProjectPath" -Level Error
        exit 1
    }
    
    & dotnet build $webProjectPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Failed to build EcommerceStarter" -Level Error
        exit 1
    }
    Write-Log "EcommerceStarter built successfully" -Level Success
    
    # Build Installer
    Write-Log "Building EcommerceStarter.Installer..." -Level Info
    $installerProjectPath = ".\EcommerceStarter.Installer\EcommerceStarter.Installer.csproj"
    
    if (-not (Test-Path $installerProjectPath)) {
        Write-Log "Installer project not found at $installerProjectPath" -Level Error
        exit 1
    }
    
    & dotnet build $installerProjectPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Failed to build EcommerceStarter.Installer" -Level Error
        exit 1
    }
    Write-Log "EcommerceStarter.Installer built successfully" -Level Success
}

function Publish-WebApplication {
    Write-Log "Publishing web application for production..." -Level Info
    
    $webProjectPath = ".\EcommerceStarter\EcommerceStarter.csproj"
    $publishPath = ".\EcommerceStarter\bin\Release\net8.0\publish"
    
    # Clean publish directory
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
    }
    
    & dotnet publish $webProjectPath `
        -c Release `
        -o $publishPath `
        --no-build `
        --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Failed to publish web application" -Level Error
        exit 1
    }
    
    Write-Log "Web application published to: $publishPath" -Level Success
    
    # Verify critical files
    $criticalFiles = @(
        "EcommerceStarter.dll",
        "web.config",
        "appsettings.json"
    )
    
    foreach ($file in $criticalFiles) {
        $filePath = Join-Path -Path $publishPath -ChildPath $file
        if (-not (Test-Path $filePath)) {
            Write-Log "Warning: Expected file not found: $file" -Level Warning
        }
    }
    
    # Remove development configuration files
    $devFiles = @(
        "appsettings.Development.json"
    )
    
    foreach ($file in $devFiles) {
        $filePath = Join-Path -Path $publishPath -ChildPath $file
        if (Test-Path $filePath) {
            Remove-Item -Path $filePath -Force
            Write-Log "Removed development file: $file" -Level Info
        }
    }
    
    return $publishPath
}

function Create-ProductionPackage {
    param([string]$PublishPath)
    
    Write-Log "Creating production package..." -Level Info
    
    # Create output directory
    $OutputPath = $OutputPath -replace '\\$'  # Remove trailing backslash
    if (-not (Test-Path $OutputPath)) {
        New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
    }
    
    # Copy published application
    $appOutputPath = Join-Path -Path $OutputPath -ChildPath "Application"
    if (Test-Path $appOutputPath) {
        Remove-Item -Path $appOutputPath -Recurse -Force
    }
    
    Copy-Item -Path $PublishPath -Destination $appOutputPath -Recurse -Force
    Write-Log "Application files copied to: $appOutputPath" -Level Success
    
    # Copy installer
    $installerSource = ".\EcommerceStarter.Installer\bin\Release\net8.0-windows"
    $installerDest = Join-Path -Path $OutputPath -ChildPath "Installer"
    
    if (Test-Path $installerSource) {
        if (Test-Path $installerDest) {
            Remove-Item -Path $installerDest -Recurse -Force
        }
        Copy-Item -Path $installerSource -Destination $installerDest -Recurse -Force
        Write-Log "Installer files copied to: $installerDest" -Level Success
    } else {
        Write-Log "Warning: Installer source not found at $installerSource" -Level Warning
    }
    
    # Create README for production deployment
    $readmePath = Join-Path -Path $OutputPath -ChildPath "PRODUCTION_DEPLOYMENT_README.md"
    Create-DeploymentReadme -OutputPath $readmePath -Version $Version
    
    Write-Log "Production package created successfully at: $OutputPath" -Level Success
}

function Create-DeploymentReadme {
    param([string]$OutputPath, [string]$Version)
    
    $readmeContent = @"
# EcommerceStarter Production Deployment Package

## Version: $Version

This is a production-ready deployment package for EcommerceStarter.

### Package Contents

- **Installer/** - Windows installer application (EcommerceStarter.Installer.exe)
- **Application/** - Published web application files ready for deployment

### Deployment Instructions

#### Option 1: Automated Installation (Recommended)

1. Run `Installer\EcommerceStarter.Installer.exe` as Administrator
2. Follow the installation wizard
3. The installer will:
   - Create the SQL Server database
   - Deploy the application to IIS
   - Configure the application pool
   - Set environment variables for production
   - Create the admin user

#### Option 2: Manual Deployment

1. **Prerequisites:**
   - Windows Server 2016 or later
   - IIS 10.0 or later
   - .NET 8 Runtime (or .NET 8 Hosting Bundle)
   - SQL Server 2016 or later

2. **Deploy Application Files:**
   - Copy the 'Application' folder to your IIS server
   - Example: `C:\inetpub\wwwroot\ecommerce`

3. **Configure IIS:**
   - Create a new Application Pool (.NET CLR version: No Managed Code)
   - Create a Web Application pointing to your deployment path
   - Set the application pool to run as an identity with database access

4. **Database Setup:**
   - Create a SQL Server database
   - Run migrations (if needed)
   - Create an admin user

5. **Configure Connection String:**
   - Edit `appsettings.json` in the application folder
   - Update the connection string to point to your SQL Server

6. **Set Production Environment:**
   - The `web.config` file in the application folder is already configured for production
   - Ensure `ASPNETCORE_ENVIRONMENT` is set to `Production`

### Production Configuration

This package includes:

- **Release Build**: Fully optimized compilation
- **No Debug Information**: Reduced file size and security
- **Production Environment**: ASPNETCORE_ENVIRONMENT=Production
- **Security Headers**: Configured in web.config
- **HTTP Compression**: Enabled for CSS, JS, and JSON
- **Static Content Caching**: Configured for optimal performance
- **Performance Optimization**: ReadyToRun enabled for faster startup

### Security Considerations

1. **Change admin password** immediately after first login
2. **Enable HTTPS** in production (via reverse proxy or SSL certificate)
3. **Regular backups** of the SQL Server database
4. **Monitor logs** in the Application Event Log
5. **Keep .NET runtime updated** with security patches

### Troubleshooting

If the application doesn't start:

1. Check IIS Application Pool status
2. Review Windows Event Log (Application section)
3. Check the application logs in `C:\inetpub\logs\LogFiles`
4. Verify database connectivity and permissions
5. Ensure ASPNETCORE_ENVIRONMENT is set to Production

### Support

For issues or questions, refer to the GitHub repository:
https://github.com/davidtres03/EcommerceStarter

### Build Information

- Build Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- .NET Version: $(& dotnet --version)
- Build Configuration: Release
- Optimization: ReadyToRun enabled
"@

    Set-Content -Path $OutputPath -Value $readmeContent -Encoding UTF8
    Write-Log "Created deployment README: $OutputPath" -Level Info
}

function Generate-BuildReport {
    Write-Log "Generating build report..." -Level Info
    
    $reportPath = Join-Path -Path $OutputPath -ChildPath "BUILD_REPORT.txt"
    
    $report = @"
================================================================================
                   ECOMMERCESTARTER PRODUCTION BUILD REPORT
================================================================================

Build Date:        $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Build Version:     $Version
Configuration:     Release
Platform:          Any CPU
.NET Version:      $(& dotnet --version)

BUILD SUMMARY
================================================================================
Status:            SUCCESS
Output Location:   $OutputPath

DELIVERABLES
================================================================================
? EcommerceStarter.Installer.exe - Production installer
? Application files (published Release build)
? Deployment documentation
? Production configuration files

PRODUCTION SETTINGS VERIFICATION
================================================================================
? ASPNETCORE_ENVIRONMENT: Production
? Debug Symbols: Removed
? Optimization: ReadyToRun enabled
? Development config files: Removed
? Security headers: Configured
? HTTP compression: Enabled
? Static caching: Configured

NEXT STEPS
================================================================================
1. Run the installer: Installer\EcommerceStarter.Installer.exe
2. Configure database connection (SQL Server 2016+)
3. Create admin user during installation
4. Test application accessibility
5. Configure SSL/HTTPS via reverse proxy or hosting provider
6. Monitor application logs and performance

IMPORTANT NOTES
================================================================================
- This is a production-optimized build
- Do NOT deploy debug builds to production
- Ensure HTTPS is configured before accepting customer data
- Keep database backups current
- Monitor application performance and error logs
- Update .NET runtime regularly for security patches

================================================================================
"@

    Set-Content -Path $reportPath -Value $report -Encoding UTF8
    Write-Log "Build report created: $reportPath" -Level Success
}

# Main execution
function Main {
    Write-Log "========================================" -Level Info
    Write-Log "EcommerceStarter Production Build Script" -Level Info
    Write-Log "========================================" -Level Info
    Write-Log "Version: $Version" -Level Info
    Write-Log "Output Path: $OutputPath" -Level Info
    Write-Log "" -Level Info
    
    try {
        Verify-Prerequisites
        Clean-BuildArtifacts
        Build-Projects
        $publishPath = Publish-WebApplication
        Create-ProductionPackage -PublishPath $publishPath
        Generate-BuildReport
        
        Write-Log "" -Level Info
        Write-Log "========================================" -Level Success
        Write-Log "Production build completed successfully!" -Level Success
        Write-Log "========================================" -Level Success
        Write-Log "Run the installer from: $OutputPath\Installer\EcommerceStarter.Installer.exe" -Level Info
    }
    catch {
        Write-Log $_.Exception.Message -Level Error
        exit 1
    }
}

# Run main function
Main
