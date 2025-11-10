# ?? EcommerceStarter Installer - Troubleshooting Guide

**Purpose:** Comprehensive troubleshooting for WPF installer issues during installation and upgrades

**Created:** 2025-11-09 01:43 AM (Catalyst Autonomous Night Work)  
**Status:** Production-Ready Troubleshooting Guide

---

## ?? Quick Diagnosis

### **Symptom-Based Quick Reference:**

**"Installer won't start"** ? [Prerequisites Issues](#prerequisites-issues)  
**"Database connection failed"** ? [Database Issues](#database-connection-issues)  
**"Admin creation failed"** ? [Admin Account Issues](#admin-account-issues)  
**"Files not copying"** ? [File System Issues](#file-system-issues)  
**"IIS configuration error"** ? [IIS Issues](#iis-configuration-issues)  
**"Upgrade detected existing data"** ? [Upgrade-Specific Issues](#upgrade-specific-issues)  
**"Installation hangs"** ? [Performance Issues](#performance-issues)  

---

## ?? Prerequisites Issues

### **Issue: ".NET 8 Not Found"**

**Symptoms:**
- Installer shows ".NET 8 SDK" with red X
- Error: "This application requires .NET 8.0 Runtime"
- Installer won't proceed past prerequisites

**Diagnosis:**
```powershell
# Check .NET installations
dotnet --list-runtimes
dotnet --list-sdks

# Expected output should include:
# Microsoft.AspNetCore.App 8.0.x
# Microsoft.NETCore.App 8.0.x
```

**Solution 1: Install .NET 8 Runtime**
```powershell
# Download .NET 8 Hosting Bundle (includes runtime + ASP.NET Core)
# URL: https://dotnet.microsoft.com/download/dotnet/8.0

# Or via winget
winget install Microsoft.DotNet.HostingBundle.8

# Restart installer after installation
```

**Solution 2: Repair Existing Installation**
```powershell
# If .NET 8 is installed but not detected
# Repair via Programs & Features
# Or reinstall hosting bundle
```

**Prevention:**
- Always install .NET 8 Hosting Bundle (not just SDK)
- Restart system after .NET installation
- Verify installation before running installer

---

### **Issue: "SQL Server Not Detected"**

**Symptoms:**
- Installer shows "SQL Server" with red X
- Cannot proceed to configuration
- Error: "No SQL Server instance found"

**Diagnosis:**
```powershell
# Check SQL Server services
Get-Service -Name "MSSQL*" | Select-Object Name, Status, StartType

# Check SQL Server instances
Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server" -Name InstalledInstances

# Test SQL Server connectivity
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

**Solution 1: Start SQL Server Service**
```powershell
# If service exists but stopped
Start-Service "MSSQL`$SQLEXPRESS"

# Set to automatic startup
Set-Service "MSSQL`$SQLEXPRESS" -StartupType Automatic
```

**Solution 2: Install SQL Server Express**
```powershell
# Download SQL Server Express
# URL: https://www.microsoft.com/sql-server/sql-server-downloads

# Or via Chocolatey
choco install sql-server-express -y

# After installation, restart installer
```

**Solution 3: Use Different SQL Server Instance**
```powershell
# If you have SQL Server (not Express)
# In installer, change server name from:
#   localhost\SQLEXPRESS
# To:
#   localhost
# Or your specific instance name
```

---

### **Issue: "IIS Not Found"**

**Symptoms:**
- Installer shows "IIS" with red X
- Error: "IIS features not installed"
- Cannot configure website

**Diagnosis:**
```powershell
# Check IIS installation
Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServer

# Check IIS service
Get-Service W3SVC
```

**Solution: Install IIS**
```powershell
# Install IIS with required features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45 -All

# Or use DISM
dism /online /enable-feature /featurename:IIS-WebServer /all

# Restart system after installation
# Then run installer again
```

---

## ??? Database Connection Issues

### **Issue: "Connection Test Failed"**

**Symptoms:**
- Red X on "Test Connection" button
- Error: "A network-related or instance-specific error occurred"
- Cannot proceed to installation

**Diagnosis:**
```powershell
# Test SQL Server connectivity
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "SELECT @@VERSION"

# If error, check:
# 1. Is SQL Server running?
Get-Service MSSQL`$SQLEXPRESS

# 2. Can we reach SQL Server?
Test-NetConnection -ComputerName localhost -Port 1433

# 3. Is TCP/IP enabled?
# Open SQL Server Configuration Manager
# SQL Server Network Configuration ? Protocols for SQLEXPRESS ? TCP/IP ? Enabled
```

**Solution 1: Enable SQL Server**
```powershell
# Start SQL Server
Start-Service MSSQL`$SQLEXPRESS

# Enable SQL Server Browser (helps with named instances)
Start-Service SQLBrowser
Set-Service SQLBrowser -StartupType Automatic
```

**Solution 2: Enable TCP/IP**
```
1. Open SQL Server Configuration Manager
2. Expand "SQL Server Network Configuration"
3. Click "Protocols for SQLEXPRESS"
4. Right-click "TCP/IP" ? Enable
5. Restart SQL Server service
```

**Solution 3: Firewall Exception**
```powershell
# Add firewall rule for SQL Server
New-NetFirewallRule -DisplayName "SQL Server" `
                    -Direction Inbound `
                    -Protocol TCP `
                    -LocalPort 1433 `
                    -Action Allow

# Add rule for SQL Browser
New-NetFirewallRule -DisplayName "SQL Browser" `
                    -Direction Inbound `
                    -Protocol UDP `
                    -LocalPort 1434 `
                    -Action Allow
```

**Solution 4: Check Connection String**
```
Server name options to try:
1. localhost\SQLEXPRESS (most common)
2. .\SQLEXPRESS (alternate syntax)
3. (localdb)\MSSQLLocalDB (LocalDB)
4. localhost (default instance)
5. 127.0.0.1\SQLEXPRESS (IP address)
```

---

### **Issue: "Database Already Exists"**

**Symptoms:**
- Error: "Database 'MyStore' already exists"
- Installation cannot proceed
- Or: Installer overwrites existing database

**Diagnosis:**
```sql
-- Check if database exists
SELECT name FROM sys.databases WHERE name = 'MyStore'

-- Check database size/contents
USE MyStore;
SELECT 
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name
```

**Solution 1: Use Different Database Name**
```
In installer configuration:
- Database name: "MyStore2" (or any unique name)
- This avoids conflict with existing database
```

**Solution 2: Upgrade Existing Database**
```
If you want to upgrade existing store:
1. Leave admin credentials EMPTY in installer
2. Installer will detect existing database
3. Skip admin creation
4. Preserve all data
5. Apply migrations only

(This is your installer's brilliant upgrade mode!)
```

**Solution 3: Delete Old Database (CAREFUL!)**
```sql
-- BACKUP FIRST!
BACKUP DATABASE MyStore TO DISK = 'C:\Backups\MyStore_Before_Delete.bak'

-- Then drop
USE master;
ALTER DATABASE MyStore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE MyStore;

-- Now run installer again
```

---

### **Issue: "Permission Denied" on Database**

**Symptoms:**
- Error: "CREATE DATABASE permission denied"
- Or: "User does not have permission"
- Connection succeeds but installation fails

**Diagnosis:**
```sql
-- Check current user
SELECT SYSTEM_USER, USER_NAME(), ORIGINAL_LOGIN()

-- Check permissions
SELECT 
    p.permission_name,
    p.state_desc
FROM sys.server_permissions p
JOIN sys.server_principals sp ON p.grantee_principal_id = sp.principal_id
WHERE sp.name = SYSTEM_USER
```

**Solution 1: Grant Permissions**
```sql
-- Run as SQL Server administrator
USE master;
GO

-- Grant create database permission
GRANT CREATE DATABASE TO [DOMAIN\Username];
GO

-- Or add to sysadmin role (full access)
ALTER SERVER ROLE sysadmin ADD MEMBER [DOMAIN\Username];
GO
```

**Solution 2: Run Installer as Administrator**
```
Right-click installer executable
? "Run as administrator"

This ensures Windows authentication has proper rights
```

**Solution 3: Use SQL Authentication**
```
In installer:
1. Change to "SQL Server Authentication"
2. Username: sa (or admin account)
3. Password: (SQL Server sa password)
4. Test connection

Note: SQL authentication must be enabled in SQL Server
```

---

## ?? Admin Account Issues

### **Issue: "Admin Creation Failed" (Duplicate Key)**

**Symptoms:**
- Error: "Violation of PRIMARY KEY constraint"
- Error: "Cannot insert duplicate key"
- Admin account not created

**Diagnosis:**
```sql
-- Check if admin users already exist
SELECT 
    u.Email,
    r.Name AS Role
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin'
```

**Solution 1: This is EXPECTED for Upgrades!**
```
Your installer already handles this! ?

From SESSION_STATE.md:
- Installer checks for existing admins
- Skips admin creation if found
- Message: "Existing admin accounts detected - skipping creation"

If you see this message: THIS IS GOOD! (Not an error)
It means installer detected upgrade scenario correctly.
```

**Solution 2: If Error Occurs Anyway**
```csharp
// Check installer code in InstallationService.cs
// Ensure this logic exists:

var existingAdmins = await context.Users
    .Join(context.UserRoles, u => u.Id, ur => ur.UserId, ...)
    .Where(r => r.Name == "Admin")
    .AnyAsync();

if (existingAdmins)
{
    // Skip creation (good!)
    return;
}

// If this logic is missing, admin creation will fail on upgrade
```

**Solution 3: Manual Skip**
```
During upgrade:
1. Leave admin email EMPTY
2. Leave admin password EMPTY
3. Installer will skip admin creation
4. Use existing admin accounts
```

---

### **Issue: "Admin Login Not Working After Installation"**

**Symptoms:**
- Installation completes successfully
- Admin email/password entered
- Login page shows "Invalid login attempt"
- Cannot access admin panel

**Diagnosis:**
```sql
-- Check if admin user was created
SELECT Email, UserName, EmailConfirmed FROM AspNetUsers

-- Check if admin role exists
SELECT Name FROM AspNetRoles WHERE Name = 'Admin'

-- Check if user is in admin role
SELECT 
    u.Email,
    r.Name
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
```

**Solution 1: Email Not Confirmed**
```sql
-- Confirm email manually
UPDATE AspNetUsers 
SET EmailConfirmed = 1 
WHERE Email = 'admin@example.com'

-- Now try logging in again
```

**Solution 2: Role Not Assigned**
```sql
-- Check roles table
SELECT * FROM AspNetRoles

-- If admin role missing, create it
INSERT INTO AspNetRoles (Id, Name, NormalizedName)
VALUES (NEWID(), 'Admin', 'ADMIN')

-- Assign user to admin role
DECLARE @UserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@example.com')
DECLARE @RoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')

INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (@UserId, @RoleId)
```

**Solution 3: Password Issue**
```csharp
// Reset password via installer or PowerShell
// Or use test admin creation script:

using Microsoft.AspNetCore.Identity;

var user = new IdentityUser
{
    UserName = "admin@example.com",
    Email = "admin@example.com",
    EmailConfirmed = true
};

var result = await userManager.CreateAsync(user, "Admin@123");
await userManager.AddToRoleAsync(user, "Admin");
```

---

## ?? File System Issues

### **Issue: "Access Denied" During File Copy**

**Symptoms:**
- Error: "Access to the path is denied"
- Files partially copied
- Installation fails at file deployment stage

**Diagnosis:**
```powershell
# Check current user permissions
whoami

# Check target directory permissions
Get-Acl "C:\inetpub\ecommercestarter" | Format-List

# Check if directory is locked
Get-Process | Where-Object { $_.Path -like "*inetpub\ecommercestarter*" }
```

**Solution 1: Run as Administrator**
```
Close installer
Right-click installer ? "Run as administrator"
Try again
```

**Solution 2: Stop IIS**
```powershell
# Stop site (releases file locks)
Stop-WebSite -Name "MyStore"
Stop-WebAppPool -Name "MyStore"

# Run installer

# Start site after installation
Start-WebAppPool -Name "MyStore"
Start-WebSite -Name "MyStore"
```

**Solution 3: Grant Permissions**
```powershell
# Grant full control to current user
$path = "C:\inetpub\ecommercestarter"
$acl = Get-Acl $path
$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$permission = $identity, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.AddAccessRule($rule)
Set-Acl $path $acl
```

---

### **Issue: "Disk Space Insufficient"**

**Symptoms:**
- Error: "There is not enough space on the disk"
- Installation fails partway through
- System becomes slow during installation

**Diagnosis:**
```powershell
# Check disk space
Get-PSDrive C | Select-Object @{N='FreeGB';E={[math]::Round($_.Free/1GB,2)}}

# EcommerceStarter requires:
# - Application files: ~100 MB
# - Database: ~50 MB (empty) to several GB (with data)
# - Logs: Variable
# Recommended: 5GB+ free
```

**Solution 1: Free Up Space**
```powershell
# Clear temp files
Remove-Item "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue

# Run disk cleanup
cleanmgr /sagerun:1

# Check space again
```

**Solution 2: Install to Different Drive**
```
In installer:
- Change installation path from C:\inetpub
- To D:\inetpub (or other drive with space)

Note: May require IIS reconfiguration
```

**Solution 3: Move Database**
```sql
-- Move database files to drive with more space
-- (Advanced - requires SQL Server knowledge)
```

---

## ?? IIS Configuration Issues

### **Issue: "IIS Website Creation Failed"**

**Symptoms:**
- Error: "Cannot create website"
- Error: "Port already in use"
- Installation completes but site not accessible

**Diagnosis:**
```powershell
# Check existing IIS sites
Get-Website | Select-Object Name, State, PhysicalPath, Bindings

# Check port usage
Get-Website | Select-Object Name, @{N='Port';E={$_.Bindings.Collection.BindingInformation.Split(':')[1]}}

# Check if port 443 or 80 is in use
Get-NetTCPConnection -LocalPort 443 -State Listen -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 80 -State Listen -ErrorAction SilentlyContinue
```

**Solution 1: Use Different Port**
```
In installer or manually:
- Change from port 80/443
- Use port 8080, 8443, or other available port

# Manual IIS configuration:
New-WebSite -Name "MyStore" -Port 8080 -PhysicalPath "C:\inetpub\ecommercestarter"
```

**Solution 2: Remove Conflicting Site**
```powershell
# Stop default website (if not needed)
Stop-WebSite -Name "Default Web Site"

# Or remove it
Remove-WebSite -Name "Default Web Site" -Confirm

# Now run installer again
```

**Solution 3: Check Application Pool**
```powershell
# If app pool exists with same name
Get-WebAppPool | Where-Object { $_.Name -eq "MyStore" }

# Remove it
Remove-WebAppPool -Name "MyStore" -Confirm

# Run installer again
```

---

### **Issue: "SSL Certificate Not Found"**

**Symptoms:**
- HTTPS site not working
- Browser shows "Not Secure"
- Error: "Certificate not found for site"

**Diagnosis:**
```powershell
# Check installed certificates
Get-ChildItem -Path "Cert:\LocalMachine\My" | 
    Select-Object Subject, Thumbprint, NotAfter

# Check IIS bindings
Get-Website -Name "MyStore" | Select-Object -ExpandProperty Bindings
```

**Solution 1: Use HTTP Instead (Development)**
```
For testing/development:
- Use http://localhost/ instead of https://
- No certificate needed
- Not secure (don't use in production!)
```

**Solution 2: Create Self-Signed Certificate**
```powershell
# Create self-signed cert
$cert = New-SelfSignedCertificate -DnsName "mystore.local" `
                                   -CertStoreLocation "Cert:\LocalMachine\My" `
                                   -FriendlyName "MyStore Dev Certificate"

# Bind to IIS site
$binding = Get-WebBinding -Name "MyStore" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "My")

# Note: Browser will show warning (self-signed)
```

**Solution 3: Import Real Certificate**
```powershell
# Import PFX certificate
$certPath = "C:\Certificates\mystore.pfx"
$certPassword = ConvertTo-SecureString -String "password" -AsPlainText -Force
Import-PfxCertificate -FilePath $certPath `
                      -CertStoreLocation "Cert:\LocalMachine\My" `
                      -Password $certPassword

# Then bind to IIS site
```

---

## ?? Upgrade-Specific Issues

### **Issue: "Upgrade Not Detected"**

**Symptoms:**
- Installer treats upgrade as fresh install
- Tries to create new admin (fails if exists)
- Overwrites settings
- Data loss risk

**Diagnosis:**
```sql
-- Check if database has data
USE MyStore;

-- Check tables
SELECT COUNT(*) FROM Products
SELECT COUNT(*) FROM AspNetUsers
SELECT COUNT(*) FROM Orders
SELECT COUNT(*) FROM Settings

-- If counts > 0, this is an existing store (upgrade scenario)
```

**Solution: Trigger Upgrade Mode**
```
During installation:
1. Database name: Use EXISTING database name
2. Test connection (should succeed)
3. Admin email: LEAVE EMPTY
4. Admin password: LEAVE EMPTY
5. Company info: LEAVE EMPTY

Installer will:
- Detect existing database
- Skip admin creation
- Preserve settings
- Apply migrations only
```

**Your Installer's Upgrade Detection:**
```csharp
// From SESSION_STATE.md - Your brilliant logic:

if (existingAdmins || string.IsNullOrEmpty(adminEmail))
{
    statusCallback("Existing admin detected or upgrade mode - skipping admin creation");
    return; // Perfect! ?
}
```

---

### **Issue: "Data Lost After Upgrade"**

**Symptoms:**
- Products missing after upgrade
- Users gone
- Orders disappeared
- Settings reset to default

**Diagnosis:**
```sql
-- Check if data was backed up
-- (Should have been done before upgrade!)

-- Check current database state
USE MyStore;
SELECT COUNT(*) FROM Products
SELECT COUNT(*) FROM AspNetUsers
SELECT COUNT(*) FROM Orders

-- Check for recent backups
SELECT 
    database_name,
    backup_start_date,
    backup_size / 1024 / 1024 AS BackupSizeMB
FROM msdb.dbo.backupset
WHERE database_name = 'MyStore'
ORDER BY backup_start_date DESC
```

**Solution: Restore from Backup**
```powershell
# CRITICAL: Always backup before upgrade!
# If you didn't backup: Data may be lost forever

# If you have backup:
$backupFile = "C:\Backups\MyStore_Before_Upgrade.bak"

# Restore database
sqlcmd -S localhost\SQLEXPRESS -E -Q "
    USE master;
    ALTER DATABASE MyStore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    RESTORE DATABASE MyStore FROM DISK = N'$backupFile' WITH REPLACE;
    ALTER DATABASE MyStore SET MULTI_USER;
"

# Then redo upgrade (but backup first this time!)
```

**Prevention:**
```
ALWAYS BEFORE UPGRADE:
1. Run Test-Migration.ps1 (creates backup)
2. Verify backup exists and is valid
3. Test restore on test database
4. THEN do actual upgrade
```

---

### **Issue: "Migration Failed"**

**Symptoms:**
- Error: "Unable to apply migration"
- Error: "Column does not exist"
- Database schema mismatch
- Site crashes after upgrade

**Diagnosis:**
```powershell
# Check migrations status
cd C:\inetpub\ecommercestarter
dotnet ef migrations list

# Check database version
sqlcmd -S localhost\SQLEXPRESS -E -d MyStore -Q "SELECT * FROM __EFMigrationsHistory"
```

**Solution 1: Apply Migrations Manually**
```powershell
cd C:\inetpub\ecommercestarter

# Generate migration script
dotnet ef migrations script --output migration.sql --idempotent

# Review script (make sure it's safe)
notepad migration.sql

# Apply to database
sqlcmd -S localhost\SQLEXPRESS -E -d MyStore -i migration.sql
```

**Solution 2: Reset Migrations (DANGEROUS)**
```sql
-- ONLY if you can afford to lose data!
DROP DATABASE MyStore;
-- Then run installer fresh
```

**Solution 3: Rollback and Retry**
```powershell
# Restore pre-upgrade backup
$backup = "C:\Backups\PreUpgrade_20251109_123456.bak"
sqlcmd -S localhost\SQLEXPRESS -E -Q "RESTORE DATABASE MyStore FROM DISK = N'$backup' WITH REPLACE"

# Fix migration issue in code
# Then try upgrade again
```

---

## ? Performance Issues

### **Issue: "Installation Takes Forever"**

**Symptoms:**
- Installation hangs at "Installing files..."
- Progress bar stuck
- Hours to complete
- System unresponsive

**Diagnosis:**
```powershell
# Check system resources
Get-Counter '\Processor(_Total)\% Processor Time'
Get-Counter '\Memory\% Committed Bytes In Use'
Get-Counter '\PhysicalDisk(_Total)\% Disk Time'

# Check installer process
Get-Process | Where-Object { $_.ProcessName -like "*Installer*" }

# Check if antivirus is scanning
# (Windows Defender or third-party)
```

**Solution 1: Disable Antivirus Temporarily**
```powershell
# Windows Defender
Set-MpPreference -DisableRealtimeMonitoring $true

# Run installer

# Re-enable after
Set-MpPreference -DisableRealtimeMonitoring $false

# Or add installer folder to exclusions
Add-MpPreference -ExclusionPath "C:\Temp\UpgradePackage"
Add-MpPreference -ExclusionPath "C:\inetpub\ecommercestarter"
```

**Solution 2: Close Other Applications**
```
- Close Visual Studio
- Close browsers
- Close SQL Management Studio
- Close heavy applications
- Then run installer
```

**Solution 3: Check Disk Performance**
```powershell
# If disk is slow (mechanical hard drive)
# Consider:
# 1. Installing to SSD
# 2. Defragmenting drive
# 3. Checking disk health (SMART status)
```

---

### **Issue: "Out of Memory During Installation"**

**Symptoms:**
- Error: "Out of memory"
- Installer crashes
- System freezes
- Blue screen (extreme cases)

**Diagnosis:**
```powershell
# Check available memory
Get-Counter '\Memory\Available MBytes'

# EcommerceStarter installer needs:
# - Minimum: 2 GB RAM
# - Recommended: 4 GB+ RAM
```

**Solution:**
```
1. Close all other applications
2. Restart computer (clears memory)
3. Run installer immediately after boot
4. If still fails: Upgrade RAM
```

---

## ?? Security Issues

### **Issue: "Windows Firewall Blocking"**

**Symptoms:**
- Site not accessible from network
- localhost works, but IP address doesn't
- Error: "Connection timed out"

**Diagnosis:**
```powershell
# Check firewall status
Get-NetFirewallProfile | Select-Object Name, Enabled

# Check if HTTP/HTTPS rules exist
Get-NetFirewallRule -DisplayName "*World Wide Web*"
```

**Solution: Add Firewall Rules**
```powershell
# Allow HTTP (port 80)
New-NetFirewallRule -DisplayName "Allow HTTP" `
                    -Direction Inbound `
                    -Protocol TCP `
                    -LocalPort 80 `
                    -Action Allow

# Allow HTTPS (port 443)
New-NetFirewallRule -DisplayName "Allow HTTPS" `
                    -Direction Inbound `
                    -Protocol TCP `
                    -LocalPort 443 `
                    -Action Allow
```

---

### **Issue: "App Pool Identity Issues"**

**Symptoms:**
- Error: "The service cannot be started"
- Error: "Logon failure"
- Website shows 503 error

**Diagnosis:**
```powershell
# Check app pool identity
Get-WebAppPoolState -Name "MyStore"
Get-ItemProperty "IIS:\AppPools\MyStore" -Name processModel.identityType
```

**Solution: Use ApplicationPoolIdentity**
```powershell
# Set app pool identity
Set-ItemProperty "IIS:\AppPools\MyStore" -Name processModel.identityType -Value "ApplicationPoolIdentity"

# Grant database permissions
# (Your Fix-Database-Permissions.ps1 script handles this!)
cd C:\Dev\Websites\Scripts
.\Fix-Database-Permissions.ps1 -AppPoolName "MyStore" -DatabaseName "MyStore"
```

---

## ?? Common Error Messages

### **"This application requires .NET Runtime 8.0"**
? Install .NET 8 Hosting Bundle  
? See [Prerequisites Issues](#prerequisites-issues)

### **"A network-related or instance-specific error occurred"**
? SQL Server not running or not accessible  
? See [Database Connection Issues](#database-connection-issues)

### **"Violation of PRIMARY KEY constraint"**
? Admin already exists (upgrade scenario)  
? See [Admin Account Issues](#admin-account-issues)  
? **This is expected for upgrades!** ?

### **"Access to the path is denied"**
? Insufficient permissions  
? See [File System Issues](#file-system-issues)

### **"Port already in use"**
? Another site using port 80/443  
? See [IIS Configuration Issues](#iis-configuration-issues)

### **"Unable to apply migration"**
? Database schema issue  
? See [Upgrade-Specific Issues](#upgrade-specific-issues)

---

## ??? Advanced Troubleshooting

### **Installer Logs**

**Location:**
```
C:\Users\[Username]\AppData\Local\Temp\EcommerceStarter.Installer.log
```

**How to enable verbose logging:**
```csharp
// In installer code, add:
logger.LogInformation("Detailed step information here");
```

**Review logs:**
```powershell
Get-Content "C:\Users\$env:USERNAME\AppData\Local\Temp\EcommerceStarter.Installer.log" -Tail 100
```

---

### **Event Viewer**

**Check Windows Event Log:**
```powershell
# Application errors
Get-EventLog -LogName Application -EntryType Error -Newest 20

# System errors
Get-EventLog -LogName System -EntryType Error -Newest 20

# IIS errors
Get-EventLog -LogName System -Source "IIS*" -Newest 20
```

---

### **IIS Logs**

**Location:**
```
C:\inetpub\logs\LogFiles\W3SVC[SiteID]\
```

**Review recent requests:**
```powershell
$latestLog = Get-ChildItem "C:\inetpub\logs\LogFiles\W3SVC*" -Recurse -File | 
             Sort-Object LastWriteTime -Descending | 
             Select-Object -First 1

Get-Content $latestLog.FullName -Tail 50
```

---

### **SQL Server Logs**

**Location:**
```
C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\Log\ERRORLOG
```

**Review:**
```powershell
Get-Content "C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\Log\ERRORLOG" -Tail 100
```

---

## ?? When All Else Fails

### **Nuclear Option: Complete Reinstall**

```powershell
# 1. Backup database
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
sqlcmd -S localhost\SQLEXPRESS -E -Q "BACKUP DATABASE MyStore TO DISK = 'C:\Backups\MyStore_$timestamp.bak'"

# 2. Remove everything
Remove-WebSite -Name "MyStore" -Confirm
Remove-WebAppPool -Name "MyStore" -Confirm
Remove-Item "C:\inetpub\ecommercestarter" -Recurse -Force

sqlcmd -S localhost\SQLEXPRESS -E -Q "DROP DATABASE MyStore"

# 3. Restart IIS
Restart-Service W3SVC

# 4. Run installer fresh

# 5. If had data: Restore database
# Then leave admin credentials empty (upgrade mode)
```

---

## ?? Getting Help

### **Before Asking for Help, Collect:**

1. **System Information:**
```powershell
# Windows version
Get-ComputerInfo | Select-Object WindowsVersion, OsArchitecture

# .NET version
dotnet --info

# SQL Server version
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

2. **Error Details:**
- Exact error message
- Screenshot of error
- Installer log file
- Event Viewer errors

3. **What You've Tried:**
- List of troubleshooting steps attempted
- Results of each attempt

---

## ? Prevention Checklist

**To avoid installer issues:**

- [ ] Install all prerequisites first
- [ ] Run installer as administrator
- [ ] Backup before upgrade
- [ ] Test on staging environment
- [ ] Close antivirus temporarily
- [ ] Ensure adequate disk space
- [ ] Verify SQL Server running
- [ ] Check IIS installed and running
- [ ] Read documentation first
- [ ] Follow upgrade guide step-by-step

---

**Created:** 2025-11-09 01:43 AM (Catalyst Autonomous Night Work Session)  
**By:** Catalyst AI (working independently)  
**Purpose:** Comprehensive installer troubleshooting  
**Status:** Production-Ready Guide ?

---

*"Every error has a solution. This guide has them all."* ??

**Troubleshoot with confidence!** ??
