# ?? EcommerceStarter - Portable Installer Guide

## Overview

The EcommerceStarter portable installer is a **completely self-contained package** that can be distributed to any Windows Server for production deployment. No source code or .NET SDK required on the target server!

---

## ?? Building the Portable Package

### On Your Development Machine:

1. **Open PowerShell** in the project root directory

2. **Run the packaging script:**
   ```powershell
   .\Build-PortableInstaller.ps1
   ```

3. **Wait for completion** (2-5 minutes)

4. **Find your package** in `.\Packages\EcommerceStarter-Installer-v1.0.0.zip`

### What Gets Created:

```
EcommerceStarter-Installer-v1.0.0/
??? EcommerceStarter.Installer.exe    ? Run this to install
??? README.txt                         ? Instructions
??? app/                               ? Pre-built application (Release mode)
?   ??? EcommerceStarter.dll
?   ??? wwwroot/
?   ??? appsettings.json
?   ??? ... (all dependencies)
??? migrations/
    ??? efbundle.exe                   ? Standalone migration tool
```

**Total package size:** ~50-70 MB (compressed)

---

## ?? Target Server Requirements

Before installing, the target server **MUST have**:

### Required Software:

| Component | Version | Download Link |
|-----------|---------|---------------|
| **Windows Server** | 2016+ (or Windows 10/11) | - |
| **IIS** | 10+ | Built into Windows |
| **ASP.NET Core Runtime** | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **SQL Server** | 2016+ | [Express](https://www.microsoft.com/sql-server/sql-server-downloads) |

### Quick Installation Commands:

Run as Administrator:

```powershell
# 1. Enable IIS
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

# 2. Download & Install ASP.NET Core Hosting Bundle from:
# https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-aspnetcore-8.0.0-windows-hosting-bundle-installer

# 3. Restart IIS
net stop was /y
net start w3svc

# 4. Verify .NET Runtime
dotnet --list-runtimes
# Should show: Microsoft.AspNetCore.App 8.x.x
```

---

## ?? Deploying to Production Server

### Step 1: Transfer Package

**Option A - USB Drive:**
1. Copy `EcommerceStarter-Installer-v1.0.0.zip` to USB
2. Plug into production server
3. Copy to `C:\Temp\` or desktop

**Option B - Network Share:**
```powershell
# On production server
Copy-Item "\\DevMachine\Share\EcommerceStarter-Installer-v1.0.0.zip" -Destination "C:\Temp\"
```

**Option C - Download:**
- Upload ZIP to your file server / cloud storage
- Download on production server

### Step 2: Extract Package

```powershell
# Extract to a working directory
Expand-Archive -Path "C:\Temp\EcommerceStarter-Installer-v1.0.0.zip" -DestinationPath "C:\Temp\Installer"
```

### Step 3: Run Installer

1. **Navigate to extracted folder:** `C:\Temp\Installer\EcommerceStarter-Installer-v1.0.0`

2. **Right-click** on `EcommerceStarter.Installer.exe`

3. Select **"Run as Administrator"**

4. **Follow the wizard:**
   - Enter company name
   - Configure database (server name, database name)
   - Set admin credentials
   - Choose installation path
   - Wait for installation (5-10 minutes)

5. **Done!** Access at `http://localhost/[YourSiteName]`

---

## ?? What the Installer Does

The installer automatically performs these steps:

### 1. **Database Setup** (Step 2 of 6)
- ? Starts SQL Browser service (if named instance)
- ? Creates database
- ? Runs EF Core migrations using bundled `efbundle.exe`
- ? Creates database schema (tables, indexes, etc.)
- ? Grants IIS application pool permissions

### 2. **Application Deployment** (Step 3 of 6)
- ? Copies pre-built files from `app/` folder
- ? Deploys to `C:\inetpub\wwwroot\[YourSiteName]`
- ? No compilation needed - everything pre-built!

### 3. **IIS Configuration** (Step 4 of 6)
- ? Creates application pool (No Managed Code mode)
- ? Creates IIS application under Default Web Site
- ? Configures app pool settings
- ? Sets file permissions

### 4. **Configuration** (Step 5 of 6)
- ? Creates `appsettings.json` with your database connection
- ? Creates `appsettings.Production.json` with production settings
- ? Generates `web.config` with IIS settings
- ? Creates admin user account using ASP.NET Identity

### 5. **Finalization** (Step 6 of 6)
- ? Registers in Windows Programs & Features
- ? Creates logs directory
- ? Starts application pool

---

## ? Post-Installation Verification

### 1. Check Website is Running

```powershell
# Check IIS
Import-Module WebAdministration
Get-WebAppPoolState -Name "[YourSiteName]"  # Should be "Started"
Get-Website -Name "Default Web Site"         # Should show your app

# Test HTTP response
Invoke-WebRequest -Uri "http://localhost/[YourSiteName]" -UseBasicParsing
```

### 2. Login to Admin Panel

1. Open browser: `http://localhost/[YourSiteName]`
2. Click **"Login"**
3. Use credentials you provided during installation
4. Navigate to Admin Dashboard

### 3. Check Database

```sql
-- In SQL Server Management Studio
USE [YourDatabaseName]

-- Verify tables exist
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
-- Should return 10+ tables

-- Verify admin user exists
SELECT Email, EmailConfirmed FROM AspNetUsers
-- Should show your admin email
```

---

## ?? Updating an Existing Installation

### To Update to a New Version:

1. **Build new portable package** on dev machine with new version:
   ```powershell
   .\Build-PortableInstaller.ps1 -Version "1.1.0"
   ```

2. **Transfer to production server**

3. **Run installer** - it will:
   - Detect existing installation
   - Backup current files
   - Update application files
   - Run new database migrations
   - Preserve your configuration
   - Keep existing admin users and data

---

## ?? Troubleshooting

### Installer Won't Start

**Error:** "EcommerceStarter.Installer.exe is not a valid Win32 application"
- **Cause:** Wrong architecture (x86 vs x64)
- **Fix:** Rebuild package on x64 machine or use `--runtime win-x64`

**Error:** "Unable to load DLL 'hostfxr.dll'"
- **Cause:** .NET Runtime not installed
- **Fix:** Install ASP.NET Core 8.0 Runtime (Hosting Bundle)

### Database Creation Fails

**Error:** "A network-related or instance-specific error occurred"
- **Cause:** SQL Server not running or wrong server name
- **Fix:** 
  ```powershell
  # Check SQL Server service
  Get-Service -Name "MSSQL*"
  
  # For named instance, start SQL Browser
  Start-Service -Name "SQLBrowser"
  Set-Service -Name "SQLBrowser" -StartupType Automatic
  ```

**Error:** "CREATE DATABASE permission denied"
- **Cause:** Windows user doesn't have permissions
- **Fix:** Add your user to SQL Server as sysadmin

### Website Shows 500 Error

**Check logs:**
```powershell
# Enable detailed errors temporarily
$webConfigPath = "C:\inetpub\wwwroot\[YourSiteName]\web.config"
(Get-Content $webConfigPath) -replace 'stdoutLogEnabled="false"', 'stdoutLogEnabled="true"' | Set-Content $webConfigPath

# Restart app pool
Restart-WebAppPool -Name "[YourSiteName]"

# Check logs
Get-Content "C:\inetpub\wwwroot\[YourSiteName]\logs\stdout_*.log" -Tail 50
```

**Common causes:**
- Database connection string incorrect
- Database not accessible
- Missing ASP.NET Core Runtime
- App pool identity doesn't have database permissions

### Application Won't Start

**Error:** "HTTP Error 502.5 - Process Failure"
- **Cause:** .NET Runtime not installed or wrong version
- **Fix:** 
  ```powershell
  # Check installed runtimes
  dotnet --list-runtimes
  
  # Should include:
  # Microsoft.AspNetCore.App 8.x.x
  ```

---

## ?? Important File Locations

| Item | Path |
|------|------|
| **Installer Package** | `.\Packages\EcommerceStarter-Installer-v1.0.0.zip` |
| **Bundled App** | (Inside package) `app\` |
| **Migration Tool** | (Inside package) `migrations\efbundle.exe` |
| **Deployed Website** | `C:\inetpub\wwwroot\[YourSiteName]\` |
| **Application Logs** | `C:\inetpub\wwwroot\[YourSiteName]\logs\` |
| **IIS Logs** | `C:\inetpub\logs\LogFiles\` |
| **Database** | SQL Server: `[YourDatabaseName]` |

---

## ?? Architecture Overview

### Development Machine (Build):
```
Source Code
    ?
Build-PortableInstaller.ps1
    ?
1. dotnet publish (Release)
2. dotnet ef migrations bundle
3. dotnet build installer
    ?
Portable Package (ZIP)
```

### Production Server (Deploy):
```
Portable Package
    ?
Extract ZIP
    ?
Run Installer.exe
    ?
1. Copy app\ ? C:\inetpub\wwwroot\
2. Run efbundle.exe (create DB)
3. Configure IIS
4. Create admin user
    ?
Website Running
```

---

## ?? Security Notes

### What's Included in the Package:
- ? Compiled binaries (DLLs)
- ? Static files (wwwroot)
- ? Migration executable
- ? **NO** source code
- ? **NO** connection strings (configured during install)
- ? **NO** passwords (provided during install)

### Safe to Distribute:
The portable package contains **no sensitive data**. You can safely:
- Email the ZIP file
- Put on USB drive
- Upload to file server
- Share with customers

### Secrets are Configured at Install Time:
- Database connection string
- Admin password
- Any API keys (Stripe, etc.)

---

## ?? Pro Tips

### 1. Version Numbering
```powershell
# Use semantic versioning
.\Build-PortableInstaller.ps1 -Version "1.2.3"

# Major.Minor.Patch
# 1 = Breaking changes
# 2 = New features
# 3 = Bug fixes
```

### 2. Custom Output Location
```powershell
# Build to network share
.\Build-PortableInstaller.ps1 -OutputPath "\\FileServer\Releases"
```

### 3. Automated Builds
```powershell
# Add to your CI/CD pipeline
$version = "1.0.$env:BUILD_NUMBER"
.\Build-PortableInstaller.ps1 -Version $version -OutputPath $env:ARTIFACTS_DIR
```

### 4. Multiple Environments
```powershell
# Create separate packages for different environments
.\Build-PortableInstaller.ps1 -Version "1.0.0-staging"
.\Build-PortableInstaller.ps1 -Version "1.0.0-production"
```

---

## ?? Success!

You now have a **completely portable, production-ready installer** that:

? **Requires NO source code** on production server  
? **Requires NO .NET SDK** on production server  
? **Includes everything needed** in one package  
? **Can be distributed anywhere** (USB, email, download)  
? **Installs in minutes** with a GUI wizard  
? **Handles updates** automatically  

**Happy Deploying! ??**
