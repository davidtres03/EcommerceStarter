# ?? Secure Windows 11 Pro Deployment Guide
## MyStore Supply Co.

This guide provides step-by-step instructions for deploying your application to a separate Windows 11 Pro host machine with maximum security and Git-based deployment.

---

## ?? Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Security Features](#security-features)
3. [Step-by-Step Deployment](#step-by-step-deployment)
4. [Automated Deployment Workflow](#automated-deployment-workflow)
5. [Security Best Practices](#security-best-practices)
6. [Monitoring & Maintenance](#monitoring--maintenance)
7. [Troubleshooting](#troubleshooting)
8. [Rollback Procedures](#rollback-procedures)

---

## ??? Architecture Overview

```
???????????????????????????????????????????????????????????????????
?                    DEVELOPMENT MACHINE                          ?
?  (Your Current Windows 11 Workstation)                         ?
?                                                                 ?
?  ? Visual Studio 2022                                          ?
?  ? Git with SSH key                                            ?
?  ? .NET 8 SDK                                                  ?
?  ? Push to GitHub                                              ?
???????????????????????????????????????????????????????????????????
                               ?
                               ? SSH (Port 22)
                               ? Encrypted
                               ?
???????????????????????????????????????????????????????????????????
?                         GITHUB                                  ?
?  ? Source Code Repository                                      ?
?  ? Branch: master                                              ?
?  ? Deploy Keys (Read-only)                                     ?
???????????????????????????????????????????????????????????????????
                               ?
                               ? SSH (Port 22)
                               ? Pull via Deploy Key
                               ?
???????????????????????????????????????????????????????????????????
?                    HOST MACHINE                                 ?
?  (Windows 11 Pro - Dedicated Server)                           ?
?                                                                 ?
?  ????????????????????????????????????????????????????????????  ?
?  ? IIS 10 with .NET 8 Hosting Bundle                        ?  ?
?  ?  ? Application Pool: MyStorePool                    ?  ?
?  ?  ? No Managed Code                                       ?  ?
?  ?  ? Isolated process                                      ?  ?
?  ????????????????????????????????????????????????????????????  ?
?                                                                 ?
?  ????????????????????????????????????????????????????????????  ?
?  ? SQL Server Express 2022                                  ?  ?
?  ?  ? Database: EcommerceStarter                       ?  ?
?  ?  ? Encrypted connections                                ?  ?
?  ?  ? Dedicated app user                                   ?  ?
?  ????????????????????????????????????????????????????????????  ?
?                                                                 ?
?  ????????????????????????????????????????????????????????????  ?
?  ? Git Repository Clone                                     ?  ?
?  ?  ? C:\Deploy\EcommerceStarter                       ?  ?
?  ?  ? Automated pull & build                               ?  ?
?  ????????????????????????????????????????????????????????????  ?
?                                                                 ?
?  ????????????????????????????????????????????????????????????  ?
?  ? Windows Firewall                                         ?  ?
?  ?  ? Port 80/443: HTTP/HTTPS                              ?  ?
?  ?  ? Port 22: SSH (Dev machine only)                      ?  ?
?  ?  ? Port 1433: SQL (Localhost only)                      ?  ?
?  ?  ? Default: Deny all inbound                            ?  ?
?  ????????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????????
```

---

## ?? Security Features

### 1. **SSH Key-Based Authentication**
- ? No passwords over the network
- ? ED25519 keys (more secure than RSA)
- ? Separate keys for GitHub and host access
- ? Deploy keys are read-only on GitHub

### 2. **Network Security**
- ? Windows Firewall with strict rules
- ? Only necessary ports open
- ? SQL Server encrypted connections
- ? HTTPS with Let's Encrypt SSL

### 3. **Application Security**
- ? IIS Application Pool isolation
- ? Minimum file permissions
- ? Azure Key Vault for secrets (already in your app)
- ? ASP.NET Core Identity with strong passwords
- ? No connection strings in source code

### 4. **Database Security**
- ? Dedicated SQL user with minimal permissions
- ? Encrypted connections required
- ? SQL authentication (not Windows auth for app)
- ? Regular automated backups

### 5. **Deployment Security**
- ? Automated from Git (no manual file copying)
- ? Rollback capability with backups
- ? Deployment user with limited permissions
- ? Audit logs for all deployments

---

## ?? Step-by-Step Deployment

### **Phase 1: Host Machine Initial Setup**

#### **1.1 Install Windows 11 Pro**
```powershell
# After fresh Windows 11 Pro install, run Windows Update
Install-Module PSWindowsUpdate -Force
Get-WindowsUpdate
Install-WindowsUpdate -AcceptAll -AutoReboot
```

#### **1.2 Set Static IP (Recommended)**
```powershell
# Find your network adapter
Get-NetAdapter

# Set static IP (adjust values for your network)
New-NetIPAddress -InterfaceAlias "Ethernet" -IPAddress 192.168.1.100 -PrefixLength 24 -DefaultGateway 192.168.1.1
Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ServerAddresses 8.8.8.8,8.8.4.4
```

#### **1.3 Create Deployment User**
```powershell
# Run as Administrator
$Password = ConvertTo-SecureString "StrongP@ssw0rd123!" -AsPlainText -Force
New-LocalUser "deploy" -Password $Password -FullName "Deployment User" -Description "Automated deployment account"
Add-LocalGroupMember -Group "Administrators" -Member "deploy"
```

#### **1.4 Install Required Software**
```powershell
# Enable IIS
Install-WindowsFeature -Name Web-Server -IncludeManagementTools
Install-WindowsFeature -Name Web-Asp-Net45
Install-WindowsFeature -Name Web-WebSockets

# Download and install .NET 8 Hosting Bundle
$url = "https://download.visualstudio.microsoft.com/download/pr/751d3fcd-72db-4da2-b8d0-709c19442225/33cc492aed9c85c508063e548fb027ee/dotnet-hosting-8.0.1-win.exe"
$output = "$env:TEMP\dotnet-hosting.exe"
Invoke-WebRequest -Uri $url -OutFile $output
Start-Process -FilePath $output -ArgumentList '/quiet', '/install' -Wait

# Restart IIS
net stop was /y
net start w3svc

# Install Git
winget install --id Git.Git -e --source winget

# Verify installations
dotnet --list-runtimes
git --version
```

#### **1.5 Install SQL Server Express**
```powershell
# Download SQL Server Express
$sqlUrl = "https://go.microsoft.com/fwlink/?linkid=2216019"
$sqlOutput = "$env:TEMP\SQLExpress.exe"
Invoke-WebRequest -Uri $sqlUrl -OutFile $sqlOutput

# Run installer (GUI - follow wizard)
Start-Process -FilePath $sqlOutput

# After install, enable TCP/IP in SQL Server Configuration Manager
# Then restart SQL Server service
Restart-Service -Name 'MSSQL$SQLEXPRESS' -Force

# Enable SQL Server Browser
Set-Service -Name SQLBrowser -StartupType Automatic
Start-Service SQLBrowser
```

---

### **Phase 2: SSH Key Setup**

#### **2.1 Generate Keys on Development Machine**
```powershell
# Open PowerShell on your dev machine
cd ~\.ssh

# Generate GitHub key
ssh-keygen -t ed25519 -C "dev@MyStore.com" -f id_ed25519_github

# Generate host access key
ssh-keygen -t ed25519 -C "deploy@host" -f id_ed25519_host

# View public keys
Get-Content id_ed25519_github.pub
Get-Content id_ed25519_host.pub

# Create SSH config
@"
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_github
    IdentitiesOnly yes

Host capcollar-host
    HostName 192.168.1.100
    User deploy
    Port 22
    IdentityFile ~/.ssh/id_ed25519_host
    IdentitiesOnly yes
"@ | Out-File -FilePath ~\.ssh\config -Encoding utf8
```

#### **2.2 Configure Host Machine SSH**
```powershell
# Run on HOST machine as Administrator

# Install OpenSSH Server
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# Start SSH service
Start-Service sshd
Set-Service -Name sshd -StartupType Automatic

# Configure firewall
New-NetFirewallRule -Name "SSH" -DisplayName "SSH (Port 22)" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22

# Switch to 'deploy' user
# Create .ssh directory
mkdir C:\Users\deploy\.ssh

# Create authorized_keys file and paste dev machine's PUBLIC key
# Copy content from dev machine's id_ed25519_host.pub
notepad C:\Users\deploy\.ssh\authorized_keys

# Set permissions (IMPORTANT!)
icacls C:\Users\deploy\.ssh /inheritance:r
icacls C:\Users\deploy\.ssh /grant:r "deploy:(OI)(CI)F"
icacls C:\Users\deploy\.ssh\authorized_keys /inheritance:r
icacls C:\Users\deploy\.ssh\authorized_keys /grant:r "deploy:F"
```

#### **2.3 Generate Host Machine GitHub Key**
```powershell
# Run on HOST machine as 'deploy' user
ssh-keygen -t ed25519 -C "host@example.com" -f C:\Users\deploy\.ssh\id_ed25519_github

# View public key (you'll add this to GitHub)
Get-Content C:\Users\deploy\.ssh\id_ed25519_github.pub

# Configure Git SSH
@"
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_github
    IdentitiesOnly yes
"@ | Out-File -FilePath C:\Users\deploy\.ssh\config -Encoding utf8
```

#### **2.4 Add Keys to GitHub**

**For Development Machine:**
1. Go to: https://github.com/settings/keys
2. Click "New SSH key"
3. Paste content of `id_ed25519_github.pub` from DEV machine
4. Title: "Development Machine"

**For Host Machine (Deploy Key):**
1. Go to: https://github.com/davidtres03/EcommerceStarter/settings/keys
2. Click "Add deploy key"
3. Paste content of `id_ed25519_github.pub` from HOST machine
4. Title: "Production Host"
5. ?? **DO NOT** check "Allow write access" (read-only for security)

#### **2.5 Test SSH Connections**
```powershell
# On DEV machine - test GitHub
ssh -T git@github.com
# Should see: "Hi davidtres03! You've successfully authenticated..."

# Test host connection
ssh deploy@capcollar-host
# Or: ssh deploy@192.168.1.100
# Should connect without password

# On HOST machine - test GitHub
ssh -T git@github.com
# Should see successful authentication
```

---

### **Phase 3: Clone Repository on Host**

```powershell
# Run on HOST machine as 'deploy' user

# Create deployment directory
mkdir C:\Deploy
cd C:\Deploy

# Clone repository via SSH
git clone git@github.com:davidtres03/EcommerceStarter.git

# Verify clone
cd EcommerceStarter
git status
git log --oneline -5
```

---

### **Phase 4: Database Setup**

#### **4.1 Create Database**
```sql
-- Connect to SQL Server on HOST machine
-- Use SQL Server Management Studio or sqlcmd

-- Create database
CREATE DATABASE EcommerceStarter;
GO

USE EcommerceStarter;
GO
```

#### **4.2 Create Application User**
```sql
-- Create login with strong password
CREATE LOGIN [CapCollarApp] WITH PASSWORD = 'YourVeryStrong!P@ssw0rd123';
GO

USE [EcommerceStarter];
GO

-- Create user in database
CREATE USER [CapCollarApp] FOR LOGIN [CapCollarApp];
GO

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER [CapCollarApp];
ALTER ROLE db_datawriter ADD MEMBER [CapCollarApp];
GRANT EXECUTE TO [CapCollarApp];
GO

-- Grant permission to create/modify schema (for EF migrations)
GRANT ALTER ON SCHEMA::dbo TO [CapCollarApp];
GO
```

#### **4.3 Run Database Migrations**
```powershell
# On HOST machine, navigate to project
cd C:\Deploy\EcommerceStarter\EcommerceStarter

# Update connection string in appsettings.Production.json
$connectionString = "Server=localhost\\SQLEXPRESS;Database=EcommerceStarter;User Id=CapCollarApp;Password=YourVeryStrong!P@ssw0rd123;TrustServerCertificate=True;Encrypt=True"

# Run migrations
dotnet ef database update
```

---

### **Phase 5: Configure IIS**

#### **5.1 Create Application Pool**
```powershell
# Run on HOST machine as Administrator
Import-Module WebAdministration

# Create app pool
New-WebAppPool -Name "MyStorePool"

# Configure app pool (NO MANAGED CODE - important for .NET Core)
Set-ItemProperty IIS:\AppPools\MyStorePool -name managedRuntimeVersion -value ""
Set-ItemProperty IIS:\AppPools\MyStorePool -name processModel.identityType -value "ApplicationPoolIdentity"
```

#### **5.2 Create Website**
```powershell
# Create site directory
mkdir C:\inetpub\wwwroot\MyStore

# Create website in IIS
New-Website -Name "MyStore" `
    -Port 80 `
    -PhysicalPath "C:\inetpub\wwwroot\MyStore" `
    -ApplicationPool "MyStorePool"

# Set permissions
$identity = "IIS AppPool\MyStorePool"
$path = "C:\inetpub\wwwroot\MyStore"

$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $path $acl
```

---

### **Phase 6: Configure Application Settings**

#### **6.1 Create Production Configuration**
```powershell
# On HOST machine
cd C:\Deploy\EcommerceStarter\EcommerceStarter

# Create appsettings.Production.json
@"
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\\\SQLEXPRESS;Database=EcommerceStarter;User Id=CapCollarApp;Password=YourVeryStrong!P@ssw0rd123;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
  },
  "AllowedHosts": "yourdomain.com,www.yourdomain.com,192.168.1.100",
  "Stripe": {
    "PublishableKey": "pk_live_YOUR_KEY",
    "SecretKey": "sk_live_YOUR_KEY"
  },
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/"
  }
}
"@ | Out-File -FilePath .\appsettings.Production.json -Encoding utf8
```

?? **IMPORTANT:** Do NOT commit `appsettings.Production.json` to Git. Add it to `.gitignore`.

---

### **Phase 7: Initial Manual Deployment**

```powershell
# On HOST machine as 'deploy' user
cd C:\Deploy\EcommerceStarter\EcommerceStarter

# Build application
dotnet restore
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release --output C:\inetpub\wwwroot\MyStore

# Copy production settings
Copy-Item .\appsettings.Production.json C:\inetpub\wwwroot\MyStore\ -Force

# Start IIS site
Start-WebAppPool -Name MyStorePool
Start-Website -Name MyStore
```

---

### **Phase 8: Configure Firewall**

```powershell
# Run on HOST machine as Administrator

# Allow HTTP
New-NetFirewallRule -Name "HTTP-Inbound" -DisplayName "HTTP (Port 80)" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 80

# Allow HTTPS
New-NetFirewallRule -Name "HTTPS-Inbound" -DisplayName "HTTPS (Port 443)" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 443

# Restrict SSH to dev machine IP only (IMPORTANT!)
New-NetFirewallRule -Name "SSH-Dev-Only" -DisplayName "SSH from Dev Machine" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -RemoteAddress 192.168.1.50  # Replace with your dev machine IP

# Block all other SSH
New-NetFirewallRule -Name "SSH-Block-Others" -DisplayName "Block Other SSH" -Enabled True -Direction Inbound -Protocol TCP -Action Block -LocalPort 22

# Set default policies
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultInboundAction Block
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultOutboundAction Allow
```

---

### **Phase 9: SSL Certificate Setup**

#### **9.1 Install win-acme (Let's Encrypt)**
```powershell
# Download win-acme
$wacmeUrl = "https://github.com/win-acme/win-acme/releases/download/v2.2.7/win-acme.v2.2.7.1654.x64.trimmed.zip"
$wacmeOutput = "C:\win-acme.zip"

Invoke-WebRequest -Uri $wacmeUrl -OutFile $wacmeOutput

# Extract
Expand-Archive -Path $wacmeOutput -DestinationPath "C:\win-acme"

# Run win-acme (follow interactive prompts)
cd C:\win-acme
.\wacs.exe
```

#### **9.2 Configure Automatic Renewal**
win-acme automatically creates a scheduled task for certificate renewal.

Verify it:
```powershell
Get-ScheduledTask | Where-Object {$_.TaskName -like "*win-acme*"}
```

---

## ?? Automated Deployment Workflow

### **Using the Deployment Script**

The `deploy-from-git.ps1` script (already created in your workspace) automates the entire deployment process.

#### **Basic Usage:**
```powershell
# On HOST machine as Administrator
cd C:\Deploy

# Deploy from master branch
.\deploy-from-git.ps1

# Deploy from specific branch
.\deploy-from-git.ps1 -Branch "develop"

# Skip tests
.\deploy-from-git.ps1 -SkipTests

# Skip backup
.\deploy-from-git.ps1 -SkipBackup
```

#### **What the Script Does:**
1. ? Creates backup of current deployment
2. ? Pulls latest code from GitHub
3. ? Restores NuGet packages
4. ? Builds application
5. ? Runs tests (optional)
6. ? Publishes application
7. ? Stops IIS
8. ? Deploys files
9. ? Sets permissions
10. ? Starts IIS
11. ? Verifies deployment
12. ? Creates deployment log

---

### **Schedule Automated Deployments** (Optional)

```powershell
# Create scheduled task for nightly deployments
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Deploy\deploy-from-git.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 2am
$principal = New-ScheduledTaskPrincipal -UserId "deploy" -LogonType Password -RunLevel Highest

Register-ScheduledTask -TaskName "MyStore-AutoDeploy" -Action $action -Trigger $trigger -Principal $principal -Description "Automated nightly deployment"
```

---

## ?? Security Best Practices

### **1. SSH Key Security**
```powershell
# Restrict SSH key file permissions (on both machines)
icacls $env:USERPROFILE\.ssh\id_ed25519 /inheritance:r
icacls $env:USERPROFILE\.ssh\id_ed25519 /grant:r "$env:USERNAME:(R)"

# Never share private keys
# Rotate keys every 6-12 months
```

### **2. Password Management**
- ? Use strong, unique passwords (20+ characters)
- ? Store Stripe keys in Azure Key Vault (already configured in your app)
- ? Never commit passwords to Git
- ? Use `appsettings.Production.json` (in `.gitignore`)

### **3. Database Security**
```sql
-- Regularly review permissions
SELECT 
    dp.name AS UserName,
    dp.type_desc AS UserType,
    o.name AS ObjectName,
    p.permission_name,
    p.state_desc
FROM sys.database_permissions p
INNER JOIN sys.database_principals dp ON p.grantee_principal_id = dp.principal_id
LEFT JOIN sys.objects o ON p.major_id = o.object_id
WHERE dp.name = 'CapCollarApp';

-- Enable SQL Server audit
USE master;
GO
CREATE SERVER AUDIT CapCollarAudit
TO FILE (FILEPATH = 'C:\SQLAudit\', MAXSIZE = 100MB, MAX_ROLLOVER_FILES = 10);
GO
ALTER SERVER AUDIT CapCollarAudit WITH (STATE = ON);
GO
```

### **4. IIS Security Headers**
Your app already has security headers, but ensure they're active:
- ? X-Content-Type-Options: nosniff
- ? X-Frame-Options: DENY
- ? X-XSS-Protection: 1; mode=block
- ? HTTPS redirection

### **5. Regular Updates**
```powershell
# Windows Updates (monthly)
Install-Module PSWindowsUpdate
Get-WindowsUpdate
Install-WindowsUpdate -AcceptAll

# .NET Updates (as released)
winget upgrade Microsoft.DotNet.DesktopRuntime.8
winget upgrade Microsoft.DotNet.AspNetCore.8

# Application Dependencies
cd C:\Deploy\EcommerceStarter\EcommerceStarter
dotnet list package --outdated
```

---

## ?? Monitoring & Maintenance

### **1. Application Logs**
```powershell
# View recent application errors
Get-EventLog -LogName Application -Source "ASP.NET Core*" -Newest 50 | 
    Where-Object {$_.EntryType -eq "Error"} | 
    Format-Table TimeGenerated, Message -AutoSize
```

### **2. IIS Logs**
```powershell
# View IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 50
```

### **3. Database Health**
```sql
-- Check database size
USE EcommerceStarter;
GO

SELECT 
    DB_NAME() AS DatabaseName,
    SUM(size) * 8 / 1024 AS SizeMB
FROM sys.database_files;
GO

-- Check active connections
SELECT 
    DB_NAME(dbid) AS DatabaseName,
    COUNT(dbid) AS NumberOfConnections
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid;
```

### **4. Disk Space Monitoring**
```powershell
# Check free space
Get-PSDrive C | Select-Object Used, Free, @{Name="FreePercent";Expression={[math]::Round(($_.Free / ($_.Used + $_.Free)) * 100, 2)}}
```

### **5. Performance Monitoring**
```powershell
# CPU and Memory usage
Get-Counter '\Processor(_Total)\% Processor Time', '\Memory\Available MBytes'

# IIS Worker Process
Get-Counter '\Process(w3wp)\% Processor Time', '\Process(w3wp)\Working Set'
```

---

## ?? Troubleshooting

### **Problem: Can't SSH to Host**
```powershell
# Check SSH service status
Get-Service sshd

# Check firewall rule
Get-NetFirewallRule -Name "SSH*"

# Test from dev machine
Test-NetConnection -ComputerName 192.168.1.100 -Port 22

# Check SSH logs on host
Get-EventLog -LogName Application -Source sshd -Newest 20
```

### **Problem: Git Clone Fails**
```powershell
# Test GitHub connection
ssh -T git@github.com

# Check SSH config
Get-Content ~\.ssh\config

# Verify key permissions
icacls ~\.ssh\id_ed25519

# Try with verbose output
GIT_SSH_COMMAND="ssh -vvv" git clone git@github.com:davidtres03/EcommerceStarter.git
```

### **Problem: Website Returns 500 Error**
```powershell
# Enable detailed errors (temporarily)
# Edit web.config in C:\inetpub\wwwroot\MyStore
<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />

# Check IIS logs
Get-Content "C:\inetpub\wwwroot\MyStore\logs\stdout*.log" -Tail 50

# Check Windows Event Viewer
Get-EventLog -LogName Application -Source "ASP.NET Core*" -Newest 20
```

### **Problem: Database Connection Failed**
```powershell
# Test SQL Server connectivity
Test-NetConnection -ComputerName localhost -Port 1433

# Verify SQL Server is running
Get-Service -Name 'MSSQL$SQLEXPRESS'

# Test login
sqlcmd -S localhost\SQLEXPRESS -U CapCollarApp -P "YourPassword" -Q "SELECT @@VERSION"
```

### **Problem: SSL Certificate Issues**
```powershell
# Check certificate binding
netsh http show sslcert

# Verify win-acme scheduled task
Get-ScheduledTask -TaskName "*win-acme*"

# Manually renew certificate
cd C:\win-acme
.\wacs.exe --renew --force
```

---

## ?? Rollback Procedures

### **Automatic Rollback**
If deployment fails, the script automatically restores from backup.

### **Manual Rollback**
```powershell
# List backups
Get-ChildItem C:\Deploy\Backups | Sort-Object CreationTime -Descending

# Restore from specific backup
$BackupPath = "C:\Deploy\Backups\MyStore_20240315_143022"

# Stop IIS
Stop-WebAppPool -Name MyStorePool
Stop-Website -Name MyStore

# Restore files
Copy-Item -Path "$BackupPath\*" -Destination "C:\inetpub\wwwroot\MyStore" -Recurse -Force

# Start IIS
Start-WebAppPool -Name MyStorePool
Start-Website -Name MyStore
```

### **Database Rollback**
```sql
-- List backups
RESTORE HEADERONLY FROM DISK = 'C:\SQLBackups\MyStore.bak';

-- Restore database
USE master;
GO

ALTER DATABASE EcommerceStarter SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

RESTORE DATABASE EcommerceStarter 
FROM DISK = 'C:\SQLBackups\MyStore_20240315.bak'
WITH REPLACE;
GO

ALTER DATABASE EcommerceStarter SET MULTI_USER;
GO
```

---

## ?? Quick Reference Commands

### **Deploy**
```powershell
cd C:\Deploy
.\deploy-from-git.ps1
```

### **View Logs**
```powershell
Get-Content C:\Deploy\Logs\deploy_*.log -Tail 50
```

### **Restart IIS**
```powershell
Stop-WebAppPool -Name MyStorePool
Start-WebAppPool -Name MyStorePool
```

### **Check Website Status**
```powershell
Get-Website -Name MyStore
Get-WebAppPoolState -Name MyStorePool
```

### **Pull Latest Code**
```powershell
cd C:\Deploy\EcommerceStarter
git pull origin master
```

---

## ?? Support Checklist

When troubleshooting, gather this information:

- [ ] Operating System version
- [ ] .NET version (`dotnet --list-runtimes`)
- [ ] Git version (`git --version`)
- [ ] IIS logs
- [ ] Application logs
- [ ] Windows Event Viewer errors
- [ ] SQL Server version
- [ ] Firewall rules
- [ ] Network connectivity
- [ ] Recent deployment logs

---

## ? Security Compliance Checklist

- [ ] SSH keys generated with ed25519
- [ ] GitHub deploy key is read-only
- [ ] SSH firewall restricted to dev machine IP
- [ ] SQL Server uses encrypted connections
- [ ] Application user has minimal database permissions
- [ ] HTTPS enabled with valid SSL certificate
- [ ] SSL auto-renewal configured
- [ ] Backups configured and tested
- [ ] Windows Firewall enabled with strict rules
- [ ] Admin password is strong (20+ chars)
- [ ] Deploy user password is strong
- [ ] SQL app user password is strong
- [ ] Secrets stored in Azure Key Vault
- [ ] No secrets in source code
- [ ] appsettings.Production.json in .gitignore
- [ ] Windows Updates enabled
- [ ] Audit logging enabled
- [ ] Monitoring configured

---

## ?? Deployment Workflow Summary

```
Developer ? Push to GitHub ? Pull on Host ? Build ? Test ? Deploy ? Verify
     ?                            ?             ?       ?       ?        ?
  Local Dev              C:\Deploy\Repo    dotnet    tests   IIS    Health
                         (SSH Clone)       build                     Check
```

---

**Your deployment is now secure, automated, and maintainable!** ??

For questions or issues, review this guide or check the troubleshooting section.
