# ?? Quick Start - Build & Deploy Your Portable Installer

## Step 1: Build the Package (On Your Dev Machine)

```powershell
# Navigate to project root
cd C:\EcommerceStater

# Run the build script
.\Build-PortableInstaller.ps1

# Wait for completion (2-5 minutes)
# ? Package will be created in .\Packages\
```

**Output:**
- `.\Packages\EcommerceStarter-Installer-v1.0.0.zip` (~50-70 MB)
- `.\Packages\EcommerceStarter-Installer-v1.0.0\` (uncompressed folder)

---

## Step 2: Transfer to Production Server

### Option A - USB Drive:
1. Copy `EcommerceStarter-Installer-v1.0.0.zip` to USB
2. Plug into production server
3. Copy to desktop

### Option B - Network:
```powershell
# On production server
Copy-Item "\\YourDevMachine\Share\Packages\EcommerceStarter-Installer-v1.0.0.zip" -Destination "C:\Temp\"
```

---

## Step 3: Install on Production Server

### Prerequisites (Must be installed first):

```powershell
# 1. Enable IIS
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

# 2. Install ASP.NET Core 8.0 Hosting Bundle
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Look for "ASP.NET Core Runtime 8.x Hosting Bundle"

# 3. Restart IIS
net stop was /y
net start w3svc
```

### Run the Installer:

1. **Extract ZIP:**
   ```powershell
   Expand-Archive -Path "C:\Temp\EcommerceStarter-Installer-v1.0.0.zip" -DestinationPath "C:\Temp\Installer"
   ```

2. **Run as Administrator:**
   - Navigate to: `C:\Temp\Installer\EcommerceStarter-Installer-v1.0.0`
   - Right-click `EcommerceStarter.Installer.exe`
   - Select **"Run as Administrator"**

3. **Follow the wizard:**
   - Company Name: *Your Company*
   - Database Server: `localhost\SQLEXPRESS` (or your SQL Server)
   - Database Name: `EcommerceStarter`
   - Admin Email: `admin@yourcompany.com`
   - Admin Password: *(choose a strong password)*
   - Site Name: `MyStore`
   - Installation Path: `C:\inetpub\wwwroot\MyStore`

4. **Wait for installation** (5-10 minutes)

5. **Done!** ??

---

## Step 4: Access Your Site

```
URL: http://localhost/MyStore
Admin Login: admin@yourcompany.com
Password: (what you entered)
```

---

## ?? Full Checklist

### On Development Machine:
- [ ] Open PowerShell in project root
- [ ] Run `.\Build-PortableInstaller.ps1`
- [ ] Verify ZIP created in `.\Packages\`
- [ ] Copy ZIP to USB or network share

### On Production Server:
- [ ] IIS installed and running
- [ ] SQL Server installed and running
- [ ] .NET 8 Runtime (Hosting Bundle) installed
- [ ] Extract installer ZIP
- [ ] Run installer as Administrator
- [ ] Complete installation wizard
- [ ] Browse to site and login

---

## ?? What Makes This Special?

? **No Source Code Needed** - Only pre-built binaries  
? **No .NET SDK Needed** - Only runtime required  
? **Single ZIP File** - Everything bundled together  
? **GUI Installer** - Professional wizard interface  
? **Automatic Database** - Creates and migrates DB automatically  
? **IIS Configuration** - Sets up everything automatically  
? **Admin Account** - Creates admin user during install  
? **Production Ready** - Optimized Release build  

---

## ?? Troubleshooting

### Build Script Fails?

**Error:** "dotnet: command not found"
- Install .NET 8 SDK on your dev machine

**Error:** "EF Core tools not found"
- Script will auto-install them (or run manually):
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

### Installer Won't Start on Server?

**Error:** "Cannot find runtime"
- Install ASP.NET Core 8.0 Hosting Bundle
- Restart IIS after installing

### Database Creation Fails?

**Error:** "Cannot connect to SQL Server"
- Verify SQL Server is running:
  ```powershell
  Get-Service -Name "MSSQL*"
  ```
- For named instances, start SQL Browser:
  ```powershell
  Start-Service -Name "SQLBrowser"
  ```

### Website Shows 500 Error?

Check logs:
```powershell
# Enable stdout logging
notepad "C:\inetpub\wwwroot\MyStore\web.config"
# Change: stdoutLogEnabled="false" to stdoutLogEnabled="true"

# Restart app pool
Restart-WebAppPool -Name "MyStore"

# View logs
Get-Content "C:\inetpub\wwwroot\MyStore\logs\stdout_*.log" -Tail 50
```

---

## ?? Need Help?

See detailed documentation:
- **Build Process:** `PORTABLE_INSTALLER_GUIDE.md`
- **Troubleshooting:** Check logs in `C:\inetpub\wwwroot\[YourSiteName]\logs\`
- **IIS Issues:** Event Viewer ? Windows Logs ? Application

---

**You're all set! Happy deploying! ??**
