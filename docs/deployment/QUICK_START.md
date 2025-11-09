# ?? Quick Start Guide
## Deploy MyStore Supply Co. to Windows 11 Pro

This is a condensed version for experienced administrators. For detailed instructions, see `SECURE_DEPLOYMENT_GUIDE.md`.

---

## ?? **Time Required**
- **First-time:** 6-8 hours
- **Experienced:** 3-4 hours

---

## ?? **Prerequisites**

? Windows 11 Pro machine (separate from dev)  
? Static IP configured (e.g., `192.168.1.100`)  
? Your dev machine IP noted (e.g., `192.168.1.50`)  
? Latest code committed and pushed to GitHub

---

## ?? **Part 1: Host Machine Setup (2-3 hours)**

### 1. Install Software (30 min)
```powershell
# Run as Administrator

# Install IIS
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

# Install .NET 8 Hosting Bundle
$url = "https://download.visualstudio.microsoft.com/download/pr/751d3fcd-72db-4da2-b8d0-709c19442225/33cc492aed9c85c508063e548fb027ee/dotnet-hosting-8.0.1-win.exe"
Invoke-WebRequest -Uri $url -OutFile "$env:TEMP\dotnet-hosting.exe"
Start-Process -FilePath "$env:TEMP\dotnet-hosting.exe" -ArgumentList '/quiet', '/install' -Wait
net stop was /y
net start w3svc

# Install Git
winget install --id Git.Git -e --source winget

# Verify
dotnet --list-runtimes
git --version
```

### 2. Install SQL Server Express (30 min)
```powershell
# Download and run installer
$sqlUrl = "https://go.microsoft.com/fwlink/?linkid=2216019"
Invoke-WebRequest -Uri $sqlUrl -OutFile "$env:TEMP\SQLExpress.exe"
Start-Process -FilePath "$env:TEMP\SQLExpress.exe"

# After install, enable TCP/IP in SQL Server Configuration Manager
# Restart SQL Server service
```

### 3. Create Deployment User (5 min)
```powershell
$Password = ConvertTo-SecureString "YourStrong!P@ssw0rd123" -AsPlainText -Force
New-LocalUser "deploy" -Password $Password -FullName "Deployment User"
Add-LocalGroupMember -Group "Administrators" -Member "deploy"
```

---

## ?? **Part 2: SSH Keys & Security (1 hour)**

### 4. Generate SSH Keys on Dev Machine (10 min)
```powershell
# On DEV machine
cd ~\.ssh
ssh-keygen -t ed25519 -C "dev@MyStore.com" -f id_ed25519_github
ssh-keygen -t ed25519 -C "deploy@host" -f id_ed25519_host

# Create config
@"
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_github

Host capcollar-host
    HostName 192.168.1.100
    User deploy
    IdentityFile ~/.ssh/id_ed25519_host
"@ | Out-File ~\.ssh\config -Encoding utf8
```

### 5. Configure SSH on Host (15 min)
```powershell
# On HOST machine as Administrator
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service -Name sshd -StartupType Automatic

# As 'deploy' user
mkdir C:\Users\deploy\.ssh
# Paste dev machine's id_ed25519_host.pub content into:
notepad C:\Users\deploy\.ssh\authorized_keys

# Set permissions
icacls C:\Users\deploy\.ssh\authorized_keys /inheritance:r
icacls C:\Users\deploy\.ssh\authorized_keys /grant:r "deploy:F"

# Generate host's GitHub key
ssh-keygen -t ed25519 -C "host@example.com" -f C:\Users\deploy\.ssh\id_ed25519_github
```

### 6. Add Keys to GitHub (5 min)
1. **Dev key:** https://github.com/settings/keys ? Add dev machine's `id_ed25519_github.pub`
2. **Deploy key:** https://github.com/davidtres03/EcommerceStarter/settings/keys ? Add host's `id_ed25519_github.pub` (**read-only!**)

### 7. Test Connections (5 min)
```powershell
# From dev machine
ssh -T git@github.com
ssh deploy@capcollar-host

# From host machine
ssh -T git@github.com
```

### 8. Run Security Hardening (20 min)
```powershell
# On HOST machine - copy security-hardening.ps1 first
.\security-hardening.ps1 -DevMachineIP "192.168.1.50"
```

---

## ?? **Part 3: Database & Application Setup (1 hour)**

### 9. Clone Repository (5 min)
```powershell
# On HOST as 'deploy' user
mkdir C:\Deploy
cd C:\Deploy
git clone git@github.com:davidtres03/EcommerceStarter.git
```

### 10. Setup Database (15 min)
```sql
-- In SSMS or sqlcmd
CREATE DATABASE EcommerceStarter;
GO

CREATE LOGIN [CapCollarApp] WITH PASSWORD = 'StrongDB!P@ss123';
GO

USE EcommerceStarter;
GO

CREATE USER [CapCollarApp] FOR LOGIN [CapCollarApp];
ALTER ROLE db_datareader ADD MEMBER [CapCollarApp];
ALTER ROLE db_datawriter ADD MEMBER [CapCollarApp];
GRANT EXECUTE TO [CapCollarApp];
GRANT ALTER ON SCHEMA::dbo TO [CapCollarApp];
GO
```

### 11. Create Production Config (10 min)
```powershell
# On HOST machine
cd C:\Deploy\EcommerceStarter\EcommerceStarter

@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\\\SQLEXPRESS;Database=EcommerceStarter;User Id=CapCollarApp;Password=StrongDB!P@ss123;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
  },
  "AllowedHosts": "*",
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
"@ | Out-File appsettings.Production.json -Encoding utf8
```

### 12. Run Migrations (5 min)
```powershell
cd C:\Deploy\EcommerceStarter\EcommerceStarter
dotnet ef database update
```

### 13. Configure IIS (10 min)
```powershell
Import-Module WebAdministration

# Create app pool
New-WebAppPool -Name "MyStorePool"
Set-ItemProperty IIS:\AppPools\MyStorePool -name managedRuntimeVersion -value ""

# Create website
New-Website -Name "MyStore" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\MyStore" -ApplicationPool "MyStorePool"
```

### 14. Initial Deploy (15 min)
```powershell
# Build and publish
cd C:\Deploy\EcommerceStarter\EcommerceStarter
dotnet publish --configuration Release --output C:\inetpub\wwwroot\MyStore

# Copy production config
Copy-Item appsettings.Production.json C:\inetpub\wwwroot\MyStore\ -Force

# Set permissions
$identity = "IIS AppPool\MyStorePool"
$acl = Get-Acl "C:\inetpub\wwwroot\MyStore"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl "C:\inetpub\wwwroot\MyStore" $acl

# Start
Start-WebAppPool -Name MyStorePool
Start-Website -Name MyStore
```

---

## ? **Part 4: Verify & Test (30 min)**

### 15. Test Website (10 min)
```powershell
# Test locally on host
curl http://localhost

# Test from dev machine
curl http://192.168.1.100
```

Visit in browser:
- ? Homepage loads
- ? Products display
- ? Shopping cart works
- ? Login/register works
- ? Admin panel accessible

### 16. Setup Automated Deployment (10 min)
```powershell
# Copy deploy-from-git.ps1 to C:\Deploy
# Test it
cd C:\Deploy
.\deploy-from-git.ps1
```

### 17. Configure SSL (Optional, 10 min)
```powershell
# Download win-acme
$url = "https://github.com/win-acme/win-acme/releases/download/v2.2.7/win-acme.v2.2.7.1654.x64.trimmed.zip"
Invoke-WebRequest -Uri $url -OutFile "C:\win-acme.zip"
Expand-Archive -Path "C:\win-acme.zip" -DestinationPath "C:\win-acme"

# Run interactive setup
cd C:\win-acme
.\wacs.exe
```

---

## ?? **Daily Operations**

### Update Application
```powershell
# On DEV machine - make changes, test, then:
git add .
git commit -m "Update feature X"
git push origin master

# On HOST machine - deploy with one command:
cd C:\Deploy
.\deploy-from-git.ps1
```

### Check Status
```powershell
# Run security check
C:\Deploy\security-check.ps1

# Check IIS
Get-WebAppPoolState -Name MyStorePool
Get-Website -Name MyStore

# Check logs
Get-EventLog -LogName Application -Source "ASP.NET Core*" -Newest 20
```

### Rollback
```powershell
# List backups
Get-ChildItem C:\Deploy\Backups

# Restore
Stop-WebAppPool -Name MyStorePool
Copy-Item -Path "C:\Deploy\Backups\MyStore_YYYYMMDD_HHMMSS\*" -Destination "C:\inetpub\wwwroot\MyStore" -Recurse -Force
Start-WebAppPool -Name MyStorePool
```

---

## ?? **Security Checklist**

After deployment, verify:

- [ ] SSH only from dev machine IP
- [ ] Firewall blocking all except 80/443
- [ ] SQL Server not accessible externally
- [ ] Strong passwords everywhere (20+ chars)
- [ ] Deploy key is read-only on GitHub
- [ ] HTTPS enabled with valid certificate
- [ ] Windows Defender enabled
- [ ] Windows Updates applied
- [ ] Backups configured
- [ ] appsettings.Production.json NOT in Git

---

## ?? **Monitoring**

### Daily
```powershell
C:\Deploy\security-check.ps1
```

### Weekly
```powershell
# Check disk space
Get-PSDrive C

# Check performance
Get-Counter '\Processor(_Total)\% Processor Time', '\Memory\Available MBytes'

# Review logs
Get-EventLog -LogName Security -After (Get-Date).AddDays(-7) | Where-Object {$_.EntryType -eq "FailureAudit"}
```

### Monthly
```powershell
# Windows Updates
Install-Module PSWindowsUpdate
Get-WindowsUpdate
Install-WindowsUpdate -AcceptAll

# .NET Updates
winget upgrade Microsoft.DotNet.DesktopRuntime.8
winget upgrade Microsoft.DotNet.AspNetCore.8
```

---

## ?? **Quick Troubleshooting**

### Website not loading?
```powershell
# Check IIS
Get-WebAppPoolState -Name MyStorePool
Get-Website -Name MyStore

# Check logs
Get-Content "C:\inetpub\wwwroot\MyStore\logs\*.log" -Tail 50

# Restart
Stop-WebAppPool -Name MyStorePool
Start-WebAppPool -Name MyStorePool
```

### Database errors?
```powershell
# Test connection
sqlcmd -S localhost\SQLEXPRESS -U CapCollarApp -P "YourPassword" -Q "SELECT @@VERSION"

# Check SQL Server is running
Get-Service -Name 'MSSQL$SQLEXPRESS'
```

### Can't SSH?
```powershell
# Check service
Get-Service sshd

# Check firewall
Get-NetFirewallRule -Name "SSH*"

# Test connection
Test-NetConnection -ComputerName 192.168.1.100 -Port 22
```

---

## ?? **Important Paths**

| Item | Path |
|------|------|
| Repository | `C:\Deploy\EcommerceStarter` |
| Website | `C:\inetpub\wwwroot\MyStore` |
| Backups | `C:\Deploy\Backups` |
| Logs | `C:\Deploy\Logs` |
| Deploy Script | `C:\Deploy\deploy-from-git.ps1` |
| Security Check | `C:\Deploy\security-check.ps1` |
| IIS Logs | `C:\inetpub\logs\LogFiles` |
| SSH Keys | `C:\Users\deploy\.ssh` |

---

## ?? **Key Commands Reference**

```powershell
# Deploy
cd C:\Deploy && .\deploy-from-git.ps1

# Security Check
C:\Deploy\security-check.ps1

# Restart IIS
iisreset

# View Logs
Get-EventLog -LogName Application -Source "ASP.NET Core*" -Newest 20

# Check Website
curl http://localhost

# Pull Latest Code
cd C:\Deploy\EcommerceStarter && git pull origin master

# Restart App Pool
Restart-WebAppPool -Name MyStorePool

# Check Disk Space
Get-PSDrive C | Select-Object Used, Free

# SQL Connection Test
sqlcmd -S localhost\SQLEXPRESS -U CapCollarApp -P "Password" -Q "SELECT 1"
```

---

## ? **Success!**

If you can:
- ? Access website from browser
- ? Login as admin
- ? View products
- ? Add to cart
- ? Run `.\deploy-from-git.ps1` successfully

**Your deployment is complete! ??**

---

## ?? **Full Documentation**

For detailed explanations, see:
- **`SECURE_DEPLOYMENT_GUIDE.md`** - Complete deployment guide with explanations
- **`DEPLOYMENT_CHECKLIST.md`** - Detailed checklist with every step
- **`deploy-from-git.ps1`** - Automated deployment script
- **`security-hardening.ps1`** - Security configuration script

---

## ?? **Pro Tips**

1. **Always test on dev first** before deploying
2. **Keep backups** - script does this automatically
3. **Monitor logs daily** - use security-check.ps1
4. **Update monthly** - Windows + .NET + packages
5. **Rotate SSH keys annually**
6. **Document any custom changes**
7. **Test rollback procedure** before you need it

---

**Questions?** Review the full guides or check troubleshooting sections.

**Good luck! ??**
