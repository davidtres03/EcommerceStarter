# ? Production Upgrade Pre-Flight Checklist

**Purpose:** Comprehensive checklist for upgrading Cap & Collar Supply Co. to EcommerceStarter latest version

**Created:** 2025-11-09 01:36 AM (Catalyst Autonomous Work)  
**Status:** Production-Ready Checklist

---

## ?? How to Use This Checklist

### **Before Starting:**
1. Print this checklist (or keep open in second monitor)
2. Check each box as you complete it
3. Don't skip steps!
4. If ANY checkbox fails ? STOP and investigate
5. Only proceed when ALL boxes checked

### **Time Estimates:**
- **Quick Upgrade:** 30-45 minutes (hot swap method)
- **Safe Upgrade:** 1-2 hours (blue-green method)
- **First Time:** Add 30 minutes (learning curve)

---

## ?? PRE-UPGRADE PREPARATION (T-24 Hours)

### **Documentation Review:**
- [ ] Read **INSTALLER-UPGRADE-GUIDE.md**
- [ ] Read **ZERO-DOWNTIME-UPGRADE.md**
- [ ] Understand rollback procedure
- [ ] Identify which strategy (blue-green vs hot swap)
- [ ] Review **Test-Migration.ps1** usage

### **Team Communication:**
- [ ] Notify stakeholders of upgrade window
- [ ] Schedule upgrade time (suggest low-traffic period)
- [ ] Have support team on standby
- [ ] Prepare status page update (if using)
- [ ] Document emergency contact info

### **Environment Verification:**
- [ ] Production site is healthy ?
  ```powershell
  Invoke-WebRequest -Uri "https://capandcollarsupplyco.com/" -UseBasicParsing
  ```
- [ ] No current issues or bugs
- [ ] All services running normally
- [ ] Recent monitoring shows stability
- [ ] Traffic patterns normal

### **Testing Complete:**
- [ ] Test-Migration.ps1 run successfully
  ```powershell
  cd C:\Dev\Websites\Scripts\Migration
  .\Test-Migration.ps1 -SourceDatabase "CapAndCollarSupplyCo"
  ```
- [ ] All 9 tests passed ?
- [ ] Test database behaves normally
- [ ] Upgrade tested on test environment
- [ ] Branding preserved in test
- [ ] Admin login works in test
- [ ] No breaking changes identified

### **Backup Strategy:**
- [ ] Backup script tested
- [ ] Backup location has space (check: 5GB+ free)
  ```powershell
  Get-PSDrive C | Select-Object Free
  ```
- [ ] Backup restoration tested (dry run)
- [ ] Off-site backup configured (optional but recommended)
- [ ] Backup retention policy documented

### **Rollback Plan:**
- [ ] Rollback procedure documented
- [ ] Rollback tested on staging
- [ ] Team knows rollback process
- [ ] Rollback can complete in < 5 minutes
- [ ] Decision criteria for rollback defined

### **Access & Permissions:**
- [ ] Remote desktop access working (192.168.1.10:3284)
- [ ] SQL Server access confirmed
- [ ] IIS management access verified
- [ ] Administrator credentials ready
- [ ] VPN connected (if remote)

---

## ??? PRE-UPGRADE SETUP (T-1 Hour)

### **Production Server Check:**
- [ ] RDP to production server successful
- [ ] Server running normally
- [ ] Disk space adequate (C:\ drive > 5GB free)
  ```powershell
  Get-PSDrive C | Select-Object @{N='FreeGB';E={[math]::Round($_.Free/1GB,2)}}
  ```
- [ ] Memory usage normal (< 80%)
  ```powershell
  Get-Counter '\Memory\% Committed Bytes In Use' | Select-Object -ExpandProperty CounterSamples
  ```
- [ ] CPU usage normal (< 50%)
  ```powershell
  Get-Counter '\Processor(_Total)\% Processor Time' | Select-Object -ExpandProperty CounterSamples
  ```

### **Services Status:**
- [ ] IIS running
  ```powershell
  Get-Service W3SVC | Select-Object Name, Status
  ```
- [ ] SQL Server running
  ```powershell
  Get-Service MSSQL`$SQLEXPRESS | Select-Object Name, Status
  ```
- [ ] Website running
  ```powershell
  Get-Website -Name "CapAndCollarSupplyCo" | Select-Object Name, State
  ```
- [ ] App pool running
  ```powershell
  Get-WebAppPoolState -Name "CapAndCollar"
  ```

### **Database Status:**
- [ ] Database accessible
  ```powershell
  sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT DB_ID('CapAndCollarSupplyCo')" -h-1
  ```
- [ ] No active long-running queries
- [ ] Database size noted (for comparison after)
  ```sql
  SELECT 
      name,
      size * 8 / 1024 AS SizeMB
  FROM sys.master_files
  WHERE database_id = DB_ID('CapAndCollarSupplyCo')
  ```
- [ ] Last backup timestamp confirmed
  ```sql
  SELECT 
      database_name,
      backup_start_date,
      type
  FROM msdb.dbo.backupset
  WHERE database_name = 'CapAndCollarSupplyCo'
  ORDER BY backup_start_date DESC
  ```

### **Upgrade Package Ready:**
- [ ] Latest EcommerceStarter code built (Release configuration)
  ```powershell
  dotnet publish -c Release -o C:\Temp\UpgradePackage\App
  ```
- [ ] Installer copied to package
  ```powershell
  Copy-Item "EcommerceStarter.Installer\bin\Release\*.exe" "C:\Temp\UpgradePackage\"
  ```
- [ ] Package transferred to production server
- [ ] Package integrity verified (MD5 hash)
  ```powershell
  Get-FileHash "C:\Temp\UpgradePackage\App\EcommerceStarter.dll" -Algorithm MD5
  ```

---

## ?? BACKUP PHASE (T-30 Minutes)

### **Database Backup:**
- [ ] Backup directory exists
  ```powershell
  New-Item -Path "C:\Backups\PreUpgrade" -ItemType Directory -Force
  ```
- [ ] Backup started
  ```powershell
  $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
  $backupPath = "C:\Backups\PreUpgrade\CapAndCollar_$timestamp.bak"
  $query = "BACKUP DATABASE [CapAndCollarSupplyCo] TO DISK = N'$backupPath' WITH COMPRESSION, INIT, FORMAT;"
  sqlcmd -S localhost\SQLEXPRESS -E -Q $query
  ```
- [ ] Backup completed successfully
- [ ] Backup file exists and size is reasonable
  ```powershell
  Get-Item $backupPath | Select-Object Name, @{N='SizeMB';E={[math]::Round($_.Length/1MB,2)}}
  ```
- [ ] Backup timestamp noted: ________________

### **Code Backup:**
- [ ] Current code directory backed up
  ```powershell
  $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
  Copy-Item "C:\inetpub\capandcollar" `
            "C:\Backups\Code_$timestamp" `
            -Recurse
  ```
- [ ] Backup completed successfully
- [ ] Backup size verified
  ```powershell
  Get-ChildItem "C:\Backups\Code_$timestamp" -Recurse | 
      Measure-Object -Property Length -Sum | 
      Select-Object @{N='SizeMB';E={[math]::Round($_.Sum/1MB,2)}}
  ```

### **Configuration Backup:**
- [ ] appsettings.Production.json backed up
  ```powershell
  Copy-Item "C:\inetpub\capandcollar\appsettings.Production.json" `
            "C:\Backups\appsettings.Production_$timestamp.json"
  ```
- [ ] IIS configuration exported
  ```powershell
  Backup-WebConfiguration -Name "PreUpgrade_$timestamp"
  ```
- [ ] SSL certificate info noted
  ```powershell
  Get-ChildItem -Path "Cert:\LocalMachine\My" | 
      Where-Object { $_.Subject -like "*capandcollarsupplyco.com*" } |
      Select-Object Subject, Thumbprint, NotAfter
  ```

### **Backup Verification:**
- [ ] All backup files exist
- [ ] Backup sizes are reasonable (not 0 bytes)
- [ ] Backup locations documented
- [ ] Backup restoration tested (dry run optional)
- [ ] Off-site copy initiated (if configured)

---

## ?? UPGRADE EXECUTION

### **METHOD A: Blue-Green Deployment (Safest - First Time)**

#### **Phase 1: Create Blue Environment**
- [ ] Blue app pool created
  ```powershell
  New-WebAppPool -Name "CapAndCollar-Blue"
  ```
- [ ] Blue website created
  ```powershell
  New-WebSite -Name "CapAndCollarSupplyCo-Blue" `
              -Port 8443 `
              -PhysicalPath "C:\inetpub\capandcollar-blue" `
              -ApplicationPool "CapAndCollar-Blue"
  ```
- [ ] SSL certificate bound
- [ ] Blue site accessible on port 8443

#### **Phase 2: Deploy to Blue**
- [ ] Files copied to Blue directory
  ```powershell
  Copy-Item "C:\Temp\UpgradePackage\App\*" `
            "C:\inetpub\capandcollar-blue\" `
            -Recurse -Force
  ```
- [ ] appsettings.Production.json copied
- [ ] File permissions set correctly
- [ ] Blue app pool started

#### **Phase 3: Database Migrations**
- [ ] Migrations reviewed
  ```powershell
  cd C:\inetpub\capandcollar-blue
  dotnet ef migrations list
  ```
- [ ] Migrations applied
  ```powershell
  dotnet ef database update
  ```
- [ ] No migration errors
- [ ] Database schema version verified

#### **Phase 4: Test Blue**
- [ ] Homepage loads (https://capandcollarsupplyco.com:8443/)
- [ ] Logo displays correctly
- [ ] Colors/theme correct (orange)
- [ ] Products display
- [ ] Product details work
- [ ] Admin login successful
- [ ] Admin dashboard accessible
- [ ] Settings preserved
- [ ] Orders visible
- [ ] Checkout flow works
- [ ] No errors in event log

#### **Phase 5: Switch Traffic**
- [ ] Current traffic noted (baseline)
- [ ] Port 443 removed from Green
  ```powershell
  Remove-WebBinding -Name "CapAndCollarSupplyCo" -Protocol https -Port 443
  ```
- [ ] Port 443 added to Blue
  ```powershell
  New-WebBinding -Name "CapAndCollarSupplyCo-Blue" -Protocol https -Port 443
  ```
- [ ] SSL certificate bound to Blue:443
- [ ] Switch timestamp noted: ________________

#### **Phase 6: Monitor Blue**
- [ ] Site accessible on https://capandcollarsupplyco.com/ (port 443)
- [ ] Homepage loads correctly
- [ ] No immediate errors
- [ ] Event log clean (no errors)
- [ ] App pool healthy
- [ ] Memory usage normal
- [ ] CPU usage normal
- [ ] Response times acceptable

---

### **METHOD B: In-Place Hot Swap (Faster - After First Success)**

#### **Phase 1: Prepare New Version**
- [ ] New version staged
  ```powershell
  Copy-Item "C:\Temp\UpgradePackage\App\*" `
            "C:\inetpub\capandcollar-new\" `
            -Recurse
  ```
- [ ] Database migrations applied
  ```powershell
  cd C:\inetpub\capandcollar-new
  dotnet ef database update
  ```

#### **Phase 2: Hot Swap (< 1 second)**
- [ ] Directory swap executed
  ```powershell
  $old = "C:\inetpub\capandcollar"
  $new = "C:\inetpub\capandcollar-new"
  $backup = "C:\inetpub\capandcollar-old"
  Rename-Item $old $backup
  Rename-Item $new $old
  ```
- [ ] Swap timestamp noted: ________________
- [ ] IIS automatically picked up changes

#### **Phase 3: Verify**
- [ ] Site loads
- [ ] No errors
- [ ] Functionality intact

---

## ?? POST-UPGRADE VERIFICATION (T+0 to T+30 Minutes)

### **Immediate Checks (First 2 Minutes):**
- [ ] Site loads at https://capandcollarsupplyco.com/
- [ ] Homepage displays correctly
- [ ] Logo shows (Cap & Collar ??)
- [ ] Colors correct (orange theme)
- [ ] No visible errors
- [ ] Response time normal (< 3 seconds)

### **Functional Testing (Minutes 2-10):**
- [ ] Browse products - all display correctly
- [ ] Click product details - pages load
- [ ] Add to cart - cart updates
- [ ] View cart - items shown correctly
- [ ] Admin login - authentication works
- [ ] Admin dashboard - accessible
- [ ] Settings page - branding preserved
- [ ] Orders page - history visible (if any)
- [ ] Checkout flow - can initiate checkout

### **Technical Verification (Minutes 10-20):**
- [ ] Event log clean
  ```powershell
  Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 20 | 
      Where-Object { $_.EntryType -eq "Error" }
  ```
- [ ] IIS logs show normal requests
  ```powershell
  Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\*.log" -Tail 20
  ```
- [ ] App pool running stably
  ```powershell
  Get-WebAppPoolState -Name "CapAndCollar-Blue" # or "CapAndCollar"
  ```
- [ ] No memory leaks (memory usage stable)
  ```powershell
  Get-Counter '\Process(w3wp)\% Processor Time'
  Get-Counter '\Process(w3wp)\Working Set'
  ```
- [ ] Database connections healthy
  ```sql
  SELECT COUNT(*) as ActiveConnections
  FROM sys.dm_exec_sessions
  WHERE database_id = DB_ID('CapAndCollarSupplyCo')
  ```

### **Performance Monitoring (Minutes 20-30):**
- [ ] Response times normal (compare to baseline)
- [ ] Page load times acceptable
- [ ] No timeouts
- [ ] No 500 errors
- [ ] No 503 errors (service unavailable)
- [ ] CPU usage < 50%
- [ ] Memory usage < 80%
- [ ] Disk I/O normal

### **Customer Impact Check:**
- [ ] No support tickets opened
- [ ] No customer complaints
- [ ] No social media mentions of issues
- [ ] Analytics show normal traffic patterns

---

## ?? SUCCESS CRITERIA

### **Upgrade is successful when ALL of these are true:**

#### **Technical Success:**
- [?] Site loads at production URL
- [?] All pages render correctly
- [?] All functionality works
- [?] No errors in event log
- [?] No errors in IIS logs
- [?] Database intact and accessible
- [?] Performance acceptable
- [?] Monitoring shows stability for 30+ minutes

#### **Business Success:**
- [?] Zero customer complaints
- [?] Zero downtime (or < 1 second)
- [?] All orders processed normally
- [?] Checkout flow functional
- [?] Admin panel accessible
- [?] Branding preserved
- [?] Settings intact

#### **Data Integrity:**
- [?] All products present
- [?] All users present
- [?] All orders present
- [?] All settings preserved
- [?] Images loading correctly
- [?] Database size unchanged (or grown appropriately)

---

## ?? ROLLBACK DECISION TREE

### **ROLLBACK if ANY of these occur:**

#### **Critical Issues (Immediate Rollback):**
- [ ] **Site completely down** (500 errors)
- [ ] **Database connection lost**
- [ ] **Checkout completely broken**
- [ ] **Data corruption detected**
- [ ] **Security vulnerability exposed**

#### **Major Issues (Rollback Recommended):**
- [ ] **Multiple 500 errors** (> 5% of requests)
- [ ] **Performance degradation** (> 2x slower)
- [ ] **Admin panel inaccessible**
- [ ] **Key features broken** (cart, checkout, login)
- [ ] **Memory leak detected** (continuous growth)

#### **Minor Issues (Monitor, Don't Rollback):**
- [ ] **Single non-critical feature broken** (e.g., product review)
- [ ] **Cosmetic issue** (CSS not loading)
- [ ] **Isolated error** (single 500 error)
- [ ] **Performance hiccup** (brief spike, then normal)

### **Rollback Execution (If Needed):**

**For Blue-Green:**
```powershell
# Switch back to Green (< 1 second)
Remove-WebBinding -Name "CapAndCollarSupplyCo-Blue" -Protocol https -Port 443
New-WebBinding -Name "CapAndCollarSupplyCo" -Protocol https -Port 443
# Bind SSL certificate back to Green
```

**For Hot Swap:**
```powershell
# Rename directories back
Rename-Item "C:\inetpub\capandcollar" "C:\inetpub\capandcollar-failed"
Rename-Item "C:\inetpub\capandcollar-old" "C:\inetpub\capandcollar"
```

**Database Rollback (If Schema Changed):**
```powershell
$backupFile = "C:\Backups\PreUpgrade\CapAndCollar_20251109_123456.bak"
sqlcmd -S localhost\SQLEXPRESS -E -Q "
    ALTER DATABASE [CapAndCollarSupplyCo] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    RESTORE DATABASE [CapAndCollarSupplyCo] FROM DISK = N'$backupFile' WITH REPLACE;
    ALTER DATABASE [CapAndCollarSupplyCo] SET MULTI_USER;
"
```

- [ ] Rollback executed successfully
- [ ] Site verified functional (old version)
- [ ] Issue documented for investigation
- [ ] Post-mortem scheduled

---

## ?? POST-UPGRADE CLEANUP (T+24 Hours)

### **After 24 hours of stable operation:**

#### **Documentation:**
- [ ] Upgrade notes documented
- [ ] Issues encountered logged
- [ ] Lessons learned recorded
- [ ] This checklist updated (if needed)
- [ ] Team debriefed

#### **Cleanup (Blue-Green Method):**
- [ ] Green environment no longer needed
- [ ] Stop Green website
  ```powershell
  Stop-WebSite -Name "CapAndCollarSupplyCo"
  ```
- [ ] Stop Green app pool
  ```powershell
  Stop-WebAppPool -Name "CapAndCollar"
  ```
- [ ] Keep Green files for 7 days (safety)
- [ ] After 7 days: Delete Green
  ```powershell
  Remove-WebSite -Name "CapAndCollarSupplyCo" -Confirm
  Remove-WebAppPool -Name "CapAndCollar" -Confirm
  Remove-Item "C:\inetpub\capandcollar" -Recurse -Confirm
  ```

#### **Backup Management:**
- [ ] Pre-upgrade backup confirmed successful
- [ ] Backup moved to archive location
- [ ] Backup retention policy applied
- [ ] Off-site backup verified

#### **Monitoring:**
- [ ] 24-hour monitoring data reviewed
- [ ] No anomalies detected
- [ ] Performance baseline updated
- [ ] Alert thresholds adjusted (if needed)

---

## ?? LESSONS LEARNED TEMPLATE

**Fill this out after upgrade:**

### **What Went Well:**
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

### **What Could Be Improved:**
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

### **Unexpected Issues:**
1. ___________________________________________
2. ___________________________________________

### **Time Estimates (Actual vs Expected):**
- **Backup Phase:** Expected: _____ Actual: _____
- **Upgrade Phase:** Expected: _____ Actual: _____
- **Testing Phase:** Expected: _____ Actual: _____
- **Total:** Expected: _____ Actual: _____

### **Would Do Differently Next Time:**
1. ___________________________________________
2. ___________________________________________

---

## ?? QUICK REFERENCE - Upgrade Day

**Print this section and keep handy:**

### **Emergency Contacts:**
- **Your Phone:** ___________________
- **Backup Contact:** ___________________
- **Hosting Provider:** ___________________
- **Database Admin:** ___________________

### **Critical Paths:**
- **Backup Location:** `C:\Backups\PreUpgrade\`
- **Code Location:** `C:\inetpub\capandcollar\`
- **Database Name:** `CapAndCollarSupplyCo`
- **Production URL:** https://capandcollarsupplyco.com/

### **Key Commands:**
```powershell
# Quick health check
Invoke-WebRequest -Uri "https://capandcollarsupplyco.com/" -UseBasicParsing

# Check event log
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 10

# Check app pool
Get-WebAppPoolState -Name "CapAndCollar-Blue"

# Rollback (blue-green)
Remove-WebBinding -Name "CapAndCollarSupplyCo-Blue" -Protocol https -Port 443
New-WebBinding -Name "CapAndCollarSupplyCo" -Protocol https -Port 443
```

---

## ? FINAL CHECKLIST SUMMARY

**Before starting upgrade, ensure:**
- [ ] All pre-upgrade prep complete
- [ ] Backup successful and verified
- [ ] Team notified
- [ ] Rollback plan ready
- [ ] Low-traffic period selected
- [ ] This checklist printed/accessible

**During upgrade:**
- [ ] Follow steps methodically
- [ ] Don't skip checks
- [ ] Document any deviations
- [ ] Monitor continuously

**After upgrade:**
- [ ] All success criteria met
- [ ] 30-minute monitoring complete
- [ ] Team notified of success
- [ ] Documentation updated

---

**Created:** 2025-11-09 01:36 AM (Catalyst Autonomous Work Session)  
**By:** Catalyst AI (working autonomously)  
**Purpose:** Comprehensive upgrade checklist  
**Status:** Production-Ready ?

---

*"A checklist is the difference between hope and confidence."* ??

**Check every box. Deploy with certainty.** ??
