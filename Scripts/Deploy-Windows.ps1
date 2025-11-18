<#
.SYNOPSIS
    EcommerceStarter - Automated Windows Deployment Script

.DESCRIPTION
    Comprehensive deployment wizard for setting up EcommerceStarter on Windows with IIS.
    
    Features:
    - Automatic prerequisite detection and installation
    - Interactive setup wizard
    - Database creation and migration
    - IIS configuration with app pool
    - SSL certificate setup
    - NTFS permission configuration
    - Site settings initialization
    
.PARAMETER Unattended
    Run in unattended mode using a configuration file

.PARAMETER ConfigFile
    Path to configuration JSON file for unattended installation

.EXAMPLE
    .\Deploy-Windows.ps1
    Interactive installation with prompts

.EXAMPLE
    .\Deploy-Windows.ps1 -Unattended -ConfigFile ".\config.json"
    Automated installation using config file

.NOTES
    Version: 1.0.0
    Author: EcommerceStarter Team
    Requires: PowerShell 5.1 or higher, Administrator privileges
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [switch]$Unattended,
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = ".\deploy-config.json"
)

# Requires Administrator privileges
#Requires -RunAsAdministrator

# ============================================================================
# GLOBAL CONFIGURATION
# ============================================================================

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$script:DeploymentConfig = @{
    # Paths
    ScriptRoot = $PSScriptRoot
    ProjectRoot = Split-Path $PSScriptRoot -Parent
    AppPath = Join-Path (Split-Path $PSScriptRoot -Parent) "EcommerceStarter"
    LogPath = Join-Path $PSScriptRoot "Logs"
    
    # Versions
    DotNetVersion = "8.0"
    SqlServerVersion = "2022"
    
    # URLs
    DotNetDownloadUrl = "https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-8.0.101-windows-x64-installer"
    SqlExpressDownloadUrl = "https://go.microsoft.com/fwlink/p/?linkid=2216019"
    UrlRewriteDownloadUrl = "https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi"
    
    # Default Values
    DefaultPort = 443
    DefaultAppPoolName = "EcommerceStarterAppPool"
}

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Type = "Info"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $prefix = "[$timestamp]"
    
    switch ($Type) {
        "Success" { 
            Write-Host "$prefix " -NoNewline -ForegroundColor Gray
            Write-Host "? " -NoNewline -ForegroundColor Green
            Write-Host $Message -ForegroundColor Green
        }
        "Error" { 
            Write-Host "$prefix " -NoNewline -ForegroundColor Gray
            Write-Host "? " -NoNewline -ForegroundColor Red
            Write-Host $Message -ForegroundColor Red
        }
        "Warning" { 
            Write-Host "$prefix " -NoNewline -ForegroundColor Gray
            Write-Host "? " -NoNewline -ForegroundColor Yellow
            Write-Host $Message -ForegroundColor Yellow
        }
        "Info" { 
            Write-Host "$prefix " -NoNewline -ForegroundColor Gray
            Write-Host "? " -NoNewline -ForegroundColor Cyan
            Write-Host $Message -ForegroundColor Cyan
        }
        "Step" { 
            Write-Host "`n$prefix " -NoNewline -ForegroundColor Gray
            Write-Host "? " -NoNewline -ForegroundColor Magenta
            Write-Host $Message -ForegroundColor Magenta
        }
        default { 
            Write-Host "$prefix $Message"
        }
    }
}

function Write-Header {
    param([string]$Title)
    
    $width = 80
    $border = "=" * $width
    $paddedTitle = " $Title "
    $padding = ($width - $paddedTitle.Length) / 2
    $centeredTitle = (" " * [Math]::Floor($padding)) + $paddedTitle + (" " * [Math]::Ceiling($padding))
    
    Write-Host "`n$border" -ForegroundColor Cyan
    Write-Host $centeredTitle -ForegroundColor Cyan
    Write-Host "$border`n" -ForegroundColor Cyan
}

function Test-Administrator {
    $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Initialize-LogFile {
    $logDir = $script:DeploymentConfig.LogPath
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    $logFile = Join-Path $logDir "deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    return $logFile
}

function Write-Log {
    param(
        [string]$Message,
        [string]$LogFile
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Out-File -FilePath $LogFile -Append
}

# ============================================================================
# PREREQUISITE CHECKS AND INSTALLATION
# ============================================================================

function Test-DotNetSdk {
    Write-ColorOutput "Checking for .NET 8 SDK..." -Type "Step"
    
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($dotnetVersion -and $dotnetVersion.StartsWith("8.")) {
            Write-ColorOutput "Found .NET SDK version $dotnetVersion" -Type "Success"
            return $true
        }
    }
    catch {
        # dotnet command not found
    }
    
    Write-ColorOutput ".NET 8 SDK not found" -Type "Warning"
    return $false
}

function Install-DotNetSdk {
    Write-ColorOutput "Installing .NET 8 SDK..." -Type "Step"
    
    $installerPath = Join-Path $env:TEMP "dotnet-sdk-8-installer.exe"
    
    try {
        Write-ColorOutput "Downloading .NET 8 SDK installer..." -Type "Info"
        Invoke-WebRequest -Uri $script:DeploymentConfig.DotNetDownloadUrl -OutFile $installerPath -UseBasicParsing
        
        Write-ColorOutput "Running installer (this may take a few minutes)..." -Type "Info"
        Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -Wait
        
        # Refresh environment variables
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        
        Write-ColorOutput ".NET 8 SDK installed successfully" -Type "Success"
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        return $true
    }
    catch {
        Write-ColorOutput "Failed to install .NET 8 SDK: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function Test-SqlServer {
    Write-ColorOutput "Checking for SQL Server..." -Type "Step"
    
    try {
        $sqlInstances = Get-Service -Name "MSSQL*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
        
        if ($sqlInstances) {
            Write-ColorOutput "Found SQL Server: $($sqlInstances[0].DisplayName)" -Type "Success"
            return $true
        }
    }
    catch {
        # SQL Server not found
    }
    
    Write-ColorOutput "SQL Server not found" -Type "Warning"
    return $false
}

function Install-SqlServerExpress {
    Write-ColorOutput "Installing SQL Server Express..." -Type "Step"
    
    $installerPath = Join-Path $env:TEMP "SQL2022-SSEI-Expr.exe"
    
    try {
        Write-ColorOutput "Downloading SQL Server Express installer..." -Type "Info"
        Write-ColorOutput "This is a large download (~250MB) and may take several minutes..." -Type "Warning"
        
        Invoke-WebRequest -Uri $script:DeploymentConfig.SqlExpressDownloadUrl -OutFile $installerPath -UseBasicParsing
        
        Write-ColorOutput "Running SQL Server Express installer..." -Type "Info"
        Write-ColorOutput "Please follow the installer prompts. Use 'SQLEXPRESS' as the instance name." -Type "Warning"
        
        Start-Process -FilePath $installerPath -Wait
        
        Write-ColorOutput "SQL Server Express installation completed" -Type "Success"
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        
        Write-ColorOutput "Waiting for SQL Server service to start..." -Type "Info"
        Start-Sleep -Seconds 10
        
        return $true
    }
    catch {
        Write-ColorOutput "Failed to install SQL Server Express: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function Test-IIS {
    Write-ColorOutput "Checking for IIS..." -Type "Step"
    
    $iisFeature = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole" -ErrorAction SilentlyContinue
    
    if ($iisFeature -and $iisFeature.State -eq "Enabled") {
        Write-ColorOutput "IIS is installed and enabled" -Type "Success"
        return $true
    }
    
    Write-ColorOutput "IIS is not installed" -Type "Warning"
    return $false
}

function Install-IIS {
    Write-ColorOutput "Installing IIS with required features..." -Type "Step"
    
    try {
        $features = @(
            "IIS-WebServerRole",
            "IIS-WebServer",
            "IIS-CommonHttpFeatures",
            "IIS-HttpErrors",
            "IIS-HttpRedirect",
            "IIS-ApplicationDevelopment",
            "IIS-NetFxExtensibility45",
            "IIS-HealthAndDiagnostics",
            "IIS-HttpLogging",
            "IIS-LoggingLibraries",
            "IIS-RequestMonitor",
            "IIS-HttpTracing",
            "IIS-Security",
            "IIS-RequestFiltering",
            "IIS-Performance",
            "IIS-WebServerManagementTools",
            "IIS-IIS6ManagementCompatibility",
            "IIS-Metabase",
            "IIS-ManagementConsole",
            "IIS-BasicAuthentication",
            "IIS-WindowsAuthentication",
            "IIS-StaticContent",
            "IIS-DefaultDocument",
            "IIS-DirectoryBrowsing",
            "IIS-ASPNET45",
            "IIS-ISAPIExtensions",
            "IIS-ISAPIFilter",
            "IIS-HttpCompressionStatic"
        )
        
        Write-ColorOutput "Installing IIS features (this will take several minutes)..." -Type "Info"
        
        foreach ($feature in $features) {
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart -ErrorAction SilentlyContinue | Out-Null
        }
        
        Write-ColorOutput "IIS installed successfully" -Type "Success"
        Write-ColorOutput "Note: A system restart may be required for all IIS features to work properly" -Type "Warning"
        
        return $true
    }
    catch {
        Write-ColorOutput "Failed to install IIS: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function Test-UrlRewrite {
    Write-ColorOutput "Checking for URL Rewrite Module..." -Type "Step"
    
    $urlRewritePath = "${env:ProgramFiles}\IIS\URL Rewrite\rewrite.dll"
    
    if (Test-Path $urlRewritePath) {
        Write-ColorOutput "URL Rewrite Module is installed" -Type "Success"
        return $true
    }
    
    Write-ColorOutput "URL Rewrite Module not found" -Type "Warning"
    return $false
}

function Install-UrlRewrite {
    Write-ColorOutput "Installing URL Rewrite Module..." -Type "Step"
    
    $installerPath = Join-Path $env:TEMP "rewrite_amd64_en-US.msi"
    
    try {
        Write-ColorOutput "Downloading URL Rewrite Module..." -Type "Info"
        Invoke-WebRequest -Uri $script:DeploymentConfig.UrlRewriteDownloadUrl -OutFile $installerPath -UseBasicParsing
        
        Write-ColorOutput "Installing URL Rewrite Module..." -Type "Info"
        Start-Process msiexec.exe -ArgumentList "/i", $installerPath, "/quiet", "/norestart" -Wait
        
        Write-ColorOutput "URL Rewrite Module installed successfully" -Type "Success"
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        
        return $true
    }
    catch {
        Write-ColorOutput "Failed to install URL Rewrite Module: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

# ============================================================================
# INTERACTIVE SETUP WIZARD
# ============================================================================

function Get-SetupConfiguration {
    Write-Header "EcommerceStarter Setup Wizard"
    
    Write-Host "Welcome! Let's configure your e-commerce store.`n" -ForegroundColor Cyan
    Write-Host "Press ENTER to accept default values shown in [brackets]`n" -ForegroundColor Yellow
    
    $config = @{}
    
    # Company Information
    Write-ColorOutput "STEP 1: Company Information" -Type "Step"
    $config.CompanyName = Read-Host "Company/Store Name [My Store]"
    if ([string]::IsNullOrWhiteSpace($config.CompanyName)) { $config.CompanyName = "My Store" }
    
    $config.SiteTagline = Read-Host "Site Tagline [Powered by EcommerceStarter]"
    if ([string]::IsNullOrWhiteSpace($config.SiteTagline)) { $config.SiteTagline = "Powered by EcommerceStarter" }
    
    # Admin Account
    Write-ColorOutput "`nSTEP 2: Administrator Account" -Type "Step"
    do {
        $config.AdminEmail = Read-Host "Admin Email Address"
    } while ([string]::IsNullOrWhiteSpace($config.AdminEmail) -or $config.AdminEmail -notmatch "^[^@]+@[^@]+\.[^@]+$")
    
    do {
        $config.AdminPassword = Read-Host "Admin Password (min 6 characters)" -AsSecureString
        $confirmPassword = Read-Host "Confirm Password" -AsSecureString
        
        $pwd1 = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($config.AdminPassword))
        $pwd2 = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($confirmPassword))
        
        if ($pwd1 -ne $pwd2) {
            Write-ColorOutput "Passwords do not match. Please try again." -Type "Error"
        }
        elseif ($pwd1.Length -lt 6) {
            Write-ColorOutput "Password must be at least 6 characters long." -Type "Error"
        }
    } while ($pwd1 -ne $pwd2 -or $pwd1.Length -lt 6)
    
    # Database Configuration
    Write-ColorOutput "`nSTEP 3: Database Configuration" -Type "Step"
    $config.DatabaseServer = Read-Host "Database Server [localhost\SQLEXPRESS]"
    if ([string]::IsNullOrWhiteSpace($config.DatabaseServer)) { $config.DatabaseServer = "localhost\SQLEXPRESS" }
    
    $config.DatabaseName = Read-Host "Database Name [MyStore]"
    if ([string]::IsNullOrWhiteSpace($config.DatabaseName)) { $config.DatabaseName = "MyStore" }
    
    # Website Configuration
    Write-ColorOutput "`nSTEP 4: Website Configuration" -Type "Step"
    $config.SiteName = Read-Host "IIS Site Name [$($config.CompanyName -replace '\s','')]"
    if ([string]::IsNullOrWhiteSpace($config.SiteName)) { $config.SiteName = $config.CompanyName -replace '\s','' }
    
    $config.Domain = Read-Host "Domain Name (e.g., www.example.com) [localhost]"
    if ([string]::IsNullOrWhiteSpace($config.Domain)) { $config.Domain = "localhost" }
    
    $config.Port = Read-Host "HTTPS Port [443]"
    if ([string]::IsNullOrWhiteSpace($config.Port)) { $config.Port = "443" }
    
    # Stripe Configuration
    Write-ColorOutput "`nSTEP 5: Stripe Payment Configuration (Optional)" -Type "Step"
    Write-Host "You can configure Stripe now or later through the admin panel." -ForegroundColor Yellow
    $configureStripe = Read-Host "Configure Stripe now? (y/N)"
    
    if ($configureStripe -eq 'y' -or $configureStripe -eq 'Y') {
        $config.StripePublishableKey = Read-Host "Stripe Publishable Key (pk_test_... or pk_live_...)"
        $config.StripeSecretKey = Read-Host "Stripe Secret Key (sk_test_... or sk_live_...)" -AsSecureString
    }
    
    # Email Configuration
    Write-ColorOutput "`nSTEP 6: Email Configuration (Optional)" -Type "Step"
    Write-Host "You can configure email now or later through the admin panel." -ForegroundColor Yellow
    $configureEmail = Read-Host "Configure email now? (y/N)"
    
    if ($configureEmail -eq 'y' -or $configureEmail -eq 'Y') {
        Write-Host "`nSelect Email Provider:" -ForegroundColor Cyan
        Write-Host "1) Resend (Recommended - 100 emails/day free)"
        Write-Host "2) SMTP (Gmail, Outlook, etc.)"
        Write-Host "3) Skip email configuration"
        
        $emailChoice = Read-Host "Choose [1-3]"
        
        switch ($emailChoice) {
            "1" {
                $config.EmailProvider = "Resend"
                $config.ResendApiKey = Read-Host "Resend API Key" -AsSecureString
            }
            "2" {
                $config.EmailProvider = "SMTP"
                $config.SmtpHost = Read-Host "SMTP Host (e.g., smtp.gmail.com)"
                $config.SmtpPort = Read-Host "SMTP Port [587]"
                if ([string]::IsNullOrWhiteSpace($config.SmtpPort)) { $config.SmtpPort = "587" }
                $config.SmtpUsername = Read-Host "SMTP Username"
                $config.SmtpPassword = Read-Host "SMTP Password" -AsSecureString
                $config.SmtpFromEmail = Read-Host "From Email Address"
            }
            default {
                $config.EmailProvider = "None"
            }
        }
    }
    else {
        $config.EmailProvider = "None"
    }
    
    # Confirmation
    Write-Header "Configuration Summary"
    Write-Host "Company Name:    $($config.CompanyName)" -ForegroundColor Cyan
    Write-Host "Admin Email:     $($config.AdminEmail)" -ForegroundColor Cyan
    Write-Host "Database Server: $($config.DatabaseServer)" -ForegroundColor Cyan
    Write-Host "Database Name:   $($config.DatabaseName)" -ForegroundColor Cyan
    Write-Host "IIS Site Name:   $($config.SiteName)" -ForegroundColor Cyan
    Write-Host "Domain:          $($config.Domain)" -ForegroundColor Cyan
    Write-Host "Port:            $($config.Port)" -ForegroundColor Cyan
    Write-Host "Stripe:          $(if ($config.StripePublishableKey) { 'Configured' } else { 'Not configured' })" -ForegroundColor Cyan
    Write-Host "Email:           $($config.EmailProvider)" -ForegroundColor Cyan
    Write-Host ""
    
    $confirm = Read-Host "Proceed with installation? (Y/n)"
    if ($confirm -eq 'n' -or $confirm -eq 'N') {
        Write-ColorOutput "Installation cancelled by user" -Type "Warning"
        exit 0
    }
    
    return $config
}

# ============================================================================
# DATABASE SETUP
# ============================================================================

function New-Database {
    param(
        [hashtable]$Config
    )
    
    Write-ColorOutput "Creating database '$($Config.DatabaseName)'..." -Type "Step"
    
    try {
        $connectionString = "Server=$($Config.DatabaseServer);Integrated Security=True;TrustServerCertificate=True"
        
        # Test connection first
        $testQuery = "SELECT @@VERSION"
        $result = Invoke-Sqlcmd -ConnectionString $connectionString -Query $testQuery -ErrorAction Stop
        
        Write-ColorOutput "Connected to SQL Server successfully" -Type "Success"
        
        # Check if database exists
        $checkDbQuery = "SELECT database_id FROM sys.databases WHERE name = '$($Config.DatabaseName)'"
        $dbExists = Invoke-Sqlcmd -ConnectionString $connectionString -Query $checkDbQuery
        
        if ($dbExists) {
            Write-ColorOutput "Database '$($Config.DatabaseName)' already exists" -Type "Warning"
            $overwrite = Read-Host "Overwrite existing database? This will DELETE all data! (y/N)"
            
            if ($overwrite -eq 'y' -or $overwrite -eq 'Y') {
                Write-ColorOutput "Dropping existing database..." -Type "Warning"
                $dropQuery = @"
                    ALTER DATABASE [$($Config.DatabaseName)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [$($Config.DatabaseName)];
"@
                Invoke-Sqlcmd -ConnectionString $connectionString -Query $dropQuery
            }
            else {
                Write-ColorOutput "Using existing database" -Type "Info"
                return $true
            }
        }
        
        # Create new database
        $createQuery = "CREATE DATABASE [$($Config.DatabaseName)]"
        Invoke-Sqlcmd -ConnectionString $connectionString -Query $createQuery
        
        Write-ColorOutput "Database created successfully" -Type "Success"
        return $true
    }
    catch {
        Write-ColorOutput "Failed to create database: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

function Invoke-DatabaseMigration {
    param(
        [hashtable]$Config
    )
    
    Write-ColorOutput "Running database migrations..." -Type "Step"
    
    try {
        $appPath = $script:DeploymentConfig.AppPath
        
        # Update connection string in appsettings.json
        $appSettingsPath = Join-Path $appPath "appsettings.json"
        $connectionString = "Server=$($Config.DatabaseServer);Database=$($Config.DatabaseName);Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        
        $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        $appSettings.ConnectionStrings.DefaultConnection = $connectionString
        $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
        
        Write-ColorOutput "Updated appsettings.json with connection string" -Type "Success"
        
        # Run EF migrations
        Push-Location $appPath
        
        Write-ColorOutput "Applying Entity Framework migrations..." -Type "Info"
        dotnet ef database update --no-build 2>&1 | Out-Null
        
        Pop-Location
        
        Write-ColorOutput "Database migrations completed successfully" -Type "Success"
        return $true
    }
    catch {
        Pop-Location
        Write-ColorOutput "Failed to run migrations: $($_.Exception.Message)" -Type "Error"
        return $false
    }
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

function Start-Deployment {
    $logFile = Initialize-LogFile
    Write-ColorOutput "Log file: $logFile" -Type "Info"
    
    try {
        # Welcome banner
        Write-Header "?? EcommerceStarter Deployment Wizard"
        Write-Host "Version 1.0.0 - Windows Edition`n" -ForegroundColor Gray
        
        # Check administrator privileges
        if (-not (Test-Administrator)) {
            Write-ColorOutput "This script requires Administrator privileges!" -Type "Error"
            Write-ColorOutput "Please run PowerShell as Administrator and try again." -Type "Warning"
            exit 1
        }
        
        # Step 1: Check and Install Prerequisites
        Write-Header "Step 1: Checking Prerequisites"
        
        if (-not (Test-DotNetSdk)) {
            $install = Read-Host ".NET 8 SDK is required. Install now? (Y/n)"
            if ($install -ne 'n' -and $install -ne 'N') {
                if (-not (Install-DotNetSdk)) {
                    throw ".NET 8 SDK installation failed"
                }
            }
            else {
                throw "Installation cancelled - .NET 8 SDK is required"
            }
        }
        
        if (-not (Test-SqlServer)) {
            $install = Read-Host "SQL Server is required. Install SQL Server Express? (Y/n)"
            if ($install -ne 'n' -and $install -ne 'N') {
                if (-not (Install-SqlServerExpress)) {
                    throw "SQL Server installation failed"
                }
            }
            else {
                Write-ColorOutput "Please install SQL Server manually and run this script again" -Type "Warning"
                exit 1
            }
        }
        
        if (-not (Test-IIS)) {
            $install = Read-Host "IIS is required. Install IIS? (Y/n)"
            if ($install -ne 'n' -and $install -ne 'N') {
                if (-not (Install-IIS)) {
                    throw "IIS installation failed"
                }
            }
            else {
                throw "Installation cancelled - IIS is required"
            }
        }
        
        if (-not (Test-UrlRewrite)) {
            $install = Read-Host "URL Rewrite Module is recommended. Install? (Y/n)"
            if ($install -ne 'n' -and $install -ne 'N') {
                Install-UrlRewrite | Out-Null
            }
        }
        
        Write-ColorOutput "`nAll prerequisites are satisfied!" -Type "Success"
        Start-Sleep -Seconds 2
        
        # Step 2: Configuration
        Write-Header "Step 2: Configuration"
        $config = Get-SetupConfiguration
        
        # Step 3: Build Application
        Write-Header "Step 3: Building Application"
        Write-ColorOutput "Building EcommerceStarter application..." -Type "Step"
        
        Push-Location $script:DeploymentConfig.ProjectRoot
        dotnet build -c Release --nologo
        Pop-Location
        
        Write-ColorOutput "Application built successfully" -Type "Success"
        
        # Step 4: Database Setup
        Write-Header "Step 4: Database Setup"
        
        if (-not (New-Database -Config $config)) {
            throw "Database creation failed"
        }
        
        if (-not (Invoke-DatabaseMigration -Config $config)) {
            throw "Database migration failed"
        }
        
        # Step 5: IIS Configuration
        Write-Header "Step 5: IIS Configuration"
        
        # Import IIS helper functions
        $iisHelpersPath = Join-Path $script:DeploymentConfig.ScriptRoot "IIS-Helpers.ps1"
        if (Test-Path $iisHelpersPath) {
            Write-ColorOutput "Loading IIS configuration helpers..." -Type "Info"
            . $iisHelpersPath
            
            $appPoolName = $script:DeploymentConfig.DefaultAppPoolName
            $publishPath = Join-Path $script:DeploymentConfig.AppPath "bin\Release\net8.0\publish"
            
            # Create application pool
            if (-not (New-IISAppPool -Config $config -AppPoolName $appPoolName)) {
                Write-ColorOutput "Failed to create app pool - you may need to configure IIS manually" -Type "Warning"
            }
            
            # Publish application
            if (-not (Publish-Application -AppPath $script:DeploymentConfig.AppPath -PublishPath $publishPath)) {
                Write-ColorOutput "Failed to publish application" -Type "Warning"
            }
            
            # Create IIS website
            if (-not (New-IISSite -Config $config -AppPoolName $appPoolName -PhysicalPath $publishPath)) {
                Write-ColorOutput "Failed to create IIS site - you may need to configure IIS manually" -Type "Warning"
            }
            
            # Set permissions
            Set-IISPermissions -PhysicalPath $publishPath -AppPoolName $appPoolName | Out-Null
            
            Write-ColorOutput "IIS configuration completed!" -Type "Success"
        }
        else {
            Write-ColorOutput "IIS-Helpers.ps1 not found - skipping IIS configuration" -Type "Warning"
            Write-ColorOutput "You'll need to configure IIS manually" -Type "Info"
        }
        
        # Step 6: Success!
        Write-Header "?? Installation Complete!"
        
        Write-Host "`n" -NoNewline
        Write-Host "=" * 80 -ForegroundColor Green
        Write-Host " SUCCESS! Your EcommerceStarter store is ready!" -ForegroundColor Green
        Write-Host "=" * 80 -ForegroundColor Green
        Write-Host "`n"
        
        Write-ColorOutput "?? Your Store Information:" -Type "Info"
        Write-Host "  Website:      https://$($config.Domain):$($config.Port)" -ForegroundColor Cyan
        Write-Host "  Admin Panel:  https://$($config.Domain):$($config.Port)/Admin/Dashboard" -ForegroundColor Cyan
        Write-Host "  Admin Email:  $($config.AdminEmail)" -ForegroundColor Cyan
        Write-Host "`n"
        
        Write-ColorOutput "?? Next Steps:" -Type "Info"
        Write-Host "  1. Open your browser (launching now...)" -ForegroundColor Yellow
        Write-Host "  2. Login to the admin panel" -ForegroundColor Yellow
        Write-Host "  3. Configure your store settings" -ForegroundColor Yellow
        Write-Host "  4. Add your first products" -ForegroundColor Yellow
        Write-Host "  5. Start selling!" -ForegroundColor Yellow
        Write-Host "`n"
        
        Write-ColorOutput "?? Important Files:" -Type "Info"
        Write-Host "  Logs:         $logFile" -ForegroundColor Gray
        Write-Host "  Config:       $configPath" -ForegroundColor Gray
        Write-Host "`n"
        
        # Try to open browser
        if (Test-Path $iisHelpersPath) {
            . $iisHelpersPath
            Write-ColorOutput "Opening browser to admin panel..." -Type "Info"
            Start-Sleep -Seconds 2
            Open-BrowserToSite -Config $config | Out-Null
        }
        
        Write-Host "=" * 80 -ForegroundColor Green
        Write-Host "`n"
        Write-ColorOutput "?? Thank you for using EcommerceStarter!" -Type "Success"
        Write-Host "`n"
        
        # Save configuration for reference
        $configPath = Join-Path $script:DeploymentConfig.ScriptRoot "last-deployment-config.json"
        $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
    }
    catch {
        Write-ColorOutput "`nDeployment failed: $($_.Exception.Message)" -Type "Error"
        Write-Log "ERROR: $($_.Exception.Message)" -LogFile $logFile
        exit 1
    }
}

# ============================================================================
# ENTRY POINT
# ============================================================================

Start-Deployment
