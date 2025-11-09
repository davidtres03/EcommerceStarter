# ?? Zero-Downtime Production Upgrade Strategy

**Goal:** Upgrade Cap & Collar Supply Co. production site with ZERO customer-facing downtime

**Created:** 2025-11-09 01:28 AM (Catalyst Autonomous Work)  
**Status:** Production-Ready Strategy

---

## ?? Why Zero-Downtime Matters

### **Business Impact:**

**Every second of downtime costs:**
- ?? Lost sales (customers can't checkout)
- ?? Customer frustration ("Why is site down?")
- ?? SEO penalties (Google doesn't like downtime)
- ?? Customer abandonment (go to competitor)
- ?? Brand trust damage

**For e-commerce:**
- Average downtime cost: **$5,600 per minute**
- Even 5 minutes = **$28,000 in lost revenue**
- Customer tolerance: **< 3 seconds** before abandoning

**Your site (https://capandcollarsupplyco.com/):**
- Currently: LIVE & STABLE ?
- Cloudflare CDN: Performance optimized
- Must maintain: 100% availability during upgrade

---

## ? Zero-Downtime is ACHIEVABLE

### **Why it works for EcommerceStarter:**

1. **Database Migrations** ?
   - Entity Framework Core handles schema changes
   - Migrations are **backwards compatible** by design
   - Old code can run on new schema (briefly)
   - New code runs on new schema (permanently)

2. **Stateless Application** ?
   - ASP.NET Core is stateless
   - Sessions in database/distributed cache
   - No in-memory state to lose
   - Requests can be served by any instance

3. **IIS Recycling** ?
   - IIS gracefully recycles app pools
   - Existing requests complete
   - New requests use new code
   - Typically < 1 second transition

4. **Code Compatibility** ?
   - Your testing proved it works!
   - CapAndCollarSupplyCo database + EcommerceStarter code = SUCCESS
   - All data preserved
   - All functionality intact

---

## ?? Three Zero-Downtime Strategies

### **Strategy 1: Blue-Green Deployment (Safest)**

**Concept:** Run two identical production environments, switch traffic between them

```
???????????????
?   USERS     ?
?  (Traffic)  ?
???????????????
       ?
       ?
????????????????????????????????????????
?      LOAD BALANCER / DNS            ?
?   (Switch traffic here)             ?
????????????????????????????????????????
       ?                       ?
       ?                       ?
???????????????         ???????????????
?   GREEN     ?         ?    BLUE     ?
? (Current)   ?         ?  (New)      ?
?  Version    ?         ?  Version    ?
?   1.0       ?         ?   1.1       ?
???????????????         ???????????????
       ?                       ?
       ?????????????????????????
                   ?
            ????????????????
            ?   DATABASE   ?
            ?  (Shared)    ?
            ????????????????
```

**Process:**

1. **Deploy to Blue (offline)**
   - Install new version on separate site
   - Test thoroughly
   - No user impact (site still on Green)

2. **Switch traffic to Blue**
   - Update load balancer/DNS
   - Instant switch
   - < 1 second disruption (DNS propagation)

3. **Monitor Blue**
   - Watch for errors
   - Verify everything works

4. **If issues: Switch back to Green**
   - Instant rollback
   - < 1 second downtime

5. **If success: Decomm Green**
   - Keep for a few days (safety)
   - Then delete old version

**Pros:**
- ? Instant rollback capability
- ? Full testing before traffic switch
- ? Zero code downtime
- ? Safest approach

**Cons:**
- ?? Requires two full environments
- ?? Database migrations must be backwards compatible
- ?? Slightly more complex setup

---

### **Strategy 2: In-Place Hot Swap (Fastest)**

**Concept:** Replace files in production site directory with atomic operations

```
BEFORE:
C:\inetpub\capandcollar\        ? IIS serves from here (Version 1.0)
C:\inetpub\capandcollar-new\    ? New version staged (Version 1.1)

SWAP (< 1 second):
Rename capandcollar ? capandcollar-old
Rename capandcollar-new ? capandcollar

AFTER:
C:\inetpub\capandcollar\        ? IIS serves from here (Version 1.1) ?
C:\inetpub\capandcollar-old\    ? Old version (rollback ready)
```

**Process:**

```powershell
# 1. Prepare new version (offline, no impact)
Copy-Item "C:\Temp\UpgradePackage\*" `
          "C:\inetpub\capandcollar-new\" `
          -Recurse

# 2. Run database migrations (online, backwards compatible)
cd C:\inetpub\capandcollar-new
dotnet ef database update

# 3. Atomic directory swap (< 1 second downtime)
$old = "C:\inetpub\capandcollar"
$new = "C:\inetpub\capandcollar-new"
$backup = "C:\inetpub\capandcollar-old"

Rename-Item $old $backup
Rename-Item $new $old

# IIS automatically picks up changes
# Existing requests complete on old app pool
# New requests use new app pool
# Total "downtime": < 1 second

# 4. Verify (if issues, swap back)
Invoke-WebRequest -Uri "https://capandcollarsupplyco.com/" -UseBasicParsing
```

**Pros:**
- ? Fastest approach
- ? Simple to execute
- ? Single server (no need for two environments)
- ? Instant rollback (rename back)

**Cons:**
- ?? Brief moment (< 1 second) of file system transition
- ?? Must handle file locks carefully
- ?? IIS recycle may take 1-2 seconds

---

### **Strategy 3: Rolling Update (Most Careful)**

**Concept:** Gradually shift traffic from old to new version

```
Step 1: 90% Old, 10% New
?????????????????????????
? 90% ? Old Version     ?
? 10% ? New Version     ?
?????????????????????????

Step 2: 50% Old, 50% New (monitor errors)
?????????????????????????
? 50% ? Old Version     ?
? 50% ? New Version     ?
?????????????????????????

Step 3: 10% Old, 90% New (confidence high)
?????????????????????????
? 10% ? Old Version     ?
? 90% ? New Version     ?
?????????????????????????

Step 4: 100% New
?????????????????????????
? 100% ? New Version ? ?
?????????????????????????
```

**Process:**

1. **Deploy new version** (separate port or subdomain)
   - https://capandcollarsupplyco.com:8080 (new)
   - https://capandcollarsupplyco.com (old, still live)

2. **Route 10% traffic to new version**
   - Load balancer configuration
   - Or Cloudflare workers
   - Monitor error rates

3. **If no errors: Increase to 50%**
   - Half on old, half on new
   - Monitor closely

4. **If still good: Increase to 90%**
   - Almost all traffic on new version

5. **If confidence high: Switch to 100%**
   - All traffic on new version
   - Old version available for rollback

**Pros:**
- ? Detect issues with minimal impact (only 10% affected)
- ? Gradual confidence building
- ? Easy to pause/rollback at any step
- ? Production validation with real traffic

**Cons:**
- ?? Requires load balancer or traffic routing
- ?? Takes longer (hours vs seconds)
- ?? More monitoring needed

---

## ?? Recommended Approach for Cap & Collar

### **For Your First Upgrade: Blue-Green (Strategy 1)**

**Why:**
- ? Safest for first production upgrade
- ? Full testing before traffic switch
- ? Instant rollback if needed
- ? You have the infrastructure (server has resources)

**Implementation:**

```powershell
# On production server (192.168.1.10)

# 1. Current site runs on:
#    - IIS Site: "CapAndCollarSupplyCo" (port 443)
#    - App Pool: "CapAndCollar"
#    - Path: C:\inetpub\capandcollar
#    - Database: CapAndCollarSupplyCo

# 2. Create Blue (new) environment
New-WebAppPool -Name "CapAndCollar-Blue"
New-WebSite -Name "CapAndCollarSupplyCo-Blue" `
            -Port 8443 `
            -PhysicalPath "C:\inetpub\capandcollar-blue" `
            -ApplicationPool "CapAndCollar-Blue"

# 3. Deploy new code to Blue
Copy-Item "C:\Temp\UpgradePackage\*" `
          "C:\inetpub\capandcollar-blue\" `
          -Recurse

# 4. Update connection string (same database)
#    Blue and Green can share database during transition
#    EF migrations are backwards compatible

# 5. Test Blue on port 8443
#    https://capandcollarsupplyco.com:8443/
#    Verify everything works

# 6. Switch IIS bindings (< 1 second)
#    Remove port 443 from Green
#    Add port 443 to Blue
#    Cloudflare continues to route traffic

# 7. Monitor Blue on port 443
#    If issues: Switch bindings back

# 8. After confidence: Delete Green
Remove-WebSite -Name "CapAndCollarSupplyCo" # Old green site
Remove-WebAppPool -Name "CapAndCollar"      # Old app pool
```

---

### **For Future Upgrades: In-Place Hot Swap (Strategy 2)**

**Once confident:**
- ? Fastest (< 1 second "downtime")
- ? Simplest (just rename directories)
- ? Battle-tested (after first upgrade success)

---

## ?? Detailed Blue-Green Implementation

### **Pre-Upgrade Preparation**

#### **1. Backup Everything**

```powershell
# Database backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Backups\PreUpgrade_$timestamp.bak"
$query = "BACKUP DATABASE [CapAndCollarSupplyCo] TO DISK = N'$backupPath' WITH COMPRESSION;"
sqlcmd -S localhost\SQLEXPRESS -E -Q $query

# Code backup
Copy-Item "C:\inetpub\capandcollar" `
          "C:\Backups\Code_$timestamp" `
          -Recurse

# IIS config backup
Backup-WebConfiguration -Name "PreUpgrade_$timestamp"
```

#### **2. Test on Staging**

```powershell
# Run Test-Migration.ps1 first
cd C:\Dev\Websites\Scripts\Migration
.\Test-Migration.ps1 -TestDatabase "CapAndCollar_UpgradeTest"

# Verify all 9 tests pass
# Verify upgrade installer logic
```

---

### **Blue Environment Setup**

#### **Step 1: Create Blue App Pool**

```powershell
Import-Module WebAdministration

# Create new app pool for Blue
New-WebAppPool -Name "CapAndCollar-Blue"

# Configure app pool (same as Green)
Set-ItemProperty "IIS:\AppPools\CapAndCollar-Blue" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty "IIS:\AppPools\CapAndCollar-Blue" -Name "enable32BitAppOnWin64" -Value $false
Set-ItemProperty "IIS:\AppPools\CapAndCollar-Blue" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# Start app pool
Start-WebAppPool -Name "CapAndCollar-Blue"
```

#### **Step 2: Create Blue Website**

```powershell
# Create physical directory
New-Item -Path "C:\inetpub\capandcollar-blue" -ItemType Directory -Force

# Create IIS site
New-WebSite -Name "CapAndCollarSupplyCo-Blue" `
            -Port 8443 `
            -HostHeader "capandcollarsupplyco.com" `
            -PhysicalPath "C:\inetpub\capandcollar-blue" `
            -ApplicationPool "CapAndCollar-Blue" `
            -Ssl

# Add SSL certificate (same as Green)
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My" | 
        Where-Object { $_.Subject -like "*capandcollarsupplyco.com*" }

New-WebBinding -Name "CapAndCollarSupplyCo-Blue" `
               -Protocol https `
               -Port 8443 `
               -HostHeader "capandcollarsupplyco.com" `
               -SslFlags 0

# Bind certificate
$binding = Get-WebBinding -Name "CapAndCollarSupplyCo-Blue" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "My")
```

#### **Step 3: Deploy Code to Blue**

```powershell
# Copy new version files
Copy-Item "C:\Temp\UpgradePackage\App\*" `
          "C:\inetpub\capandcollar-blue\" `
          -Recurse -Force

# Copy appsettings (same connection string)
Copy-Item "C:\inetpub\capandcollar\appsettings.Production.json" `
          "C:\inetpub\capandcollar-blue\appsettings.Production.json"

# Set permissions
$acl = Get-Acl "C:\inetpub\capandcollar-blue"
$identity = "IIS AppPool\CapAndCollar-Blue"
$permission = $identity, "Read,Execute", "ContainerInherit,ObjectInherit", "None", "Allow"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.AddAccessRule($rule)
Set-Acl "C:\inetpub\capandcollar-blue" $acl
```

#### **Step 4: Run Migrations on Blue**

```powershell
cd C:\inetpub\capandcollar-blue

# Apply migrations (safe - backwards compatible)
dotnet ef database update

# Migrations run against shared database
# Both Green and Blue can work with migrated schema
```

---

### **Testing Blue Environment**

#### **Internal Testing (Port 8443)**

```powershell
# Test homepage
Invoke-WebRequest -Uri "https://capandcollarsupplyco.com:8443/" `
                  -UseBasicParsing

# Test admin login
Start-Process "https://capandcollarsupplyco.com:8443/Admin/Dashboard"
```

**Manual Verification:**
- [ ] Homepage loads
- [ ] Logo displays (Cap & Collar ??)
- [ ] Colors correct (orange)
- [ ] Products show
- [ ] Product details work
- [ ] Admin login successful
- [ ] Admin dashboard accessible
- [ ] Settings preserved
- [ ] Orders visible
- [ ] Checkout flow works
- [ ] No errors in event log

#### **Load Testing**

```powershell
# Simple load test
1..100 | ForEach-Object -Parallel {
    Invoke-WebRequest -Uri "https://capandcollarsupplyco.com:8443/" `
                      -UseBasicParsing
} -ThrottleLimit 10

# Check response times, memory usage, errors
```

---

### **Traffic Switch (The Big Moment)**

#### **Preparation:**

```powershell
# 1. Verify Blue is healthy
Get-Website -Name "CapAndCollarSupplyCo-Blue" | Select-Object Name, State

# 2. Verify Green is still running (fallback)
Get-Website -Name "CapAndCollarSupplyCo" | Select-Object Name, State

# 3. Note current traffic (for comparison)
Get-Counter -Counter "\Web Service(CapAndCollarSupplyCo)\Current Connections"
```

#### **Execute Switch:**

```powershell
# ATOMIC SWITCH (< 1 second total)

# Step 1: Remove port 443 from Green
Remove-WebBinding -Name "CapAndCollarSupplyCo" `
                  -Protocol https `
                  -Port 443

# Step 2: Add port 443 to Blue
New-WebBinding -Name "CapAndCollarSupplyCo-Blue" `
               -Protocol https `
               -Port 443 `
               -HostHeader "capandcollarsupplyco.com"

# Step 3: Bind SSL certificate
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My" | 
        Where-Object { $_.Subject -like "*capandcollarsupplyco.com*" }
$binding = Get-WebBinding -Name "CapAndCollarSupplyCo-Blue" -Protocol https -Port 443
$binding.AddSslCertificate($cert.Thumbprint, "My")

# DONE! Blue is now serving production traffic on port 443
# Total elapsed time: < 1 second
# Customer impact: ZERO (Cloudflare CDN handles seamlessly)
```

---

### **Post-Switch Monitoring**

#### **Immediate Checks (First 5 minutes):**

```powershell
# 1. Verify site responds
Invoke-WebRequest -Uri "https://capandcollarsupplyco.com/" -UseBasicParsing

# 2. Check event log for errors
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 20 | 
    Where-Object { $_.EntryType -eq "Error" }

# 3. Monitor app pool
Get-WebAppPoolState -Name "CapAndCollar-Blue"

# 4. Check active connections
Get-Counter -Counter "\Web Service(CapAndCollarSupplyCo-Blue)\Current Connections"

# 5. Watch IIS logs in real-time
Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\*.log" -Tail 20 -Wait
```

#### **Extended Monitoring (First 30 minutes):**

- [ ] Traffic patterns normal
- [ ] Response times acceptable
- [ ] No 500 errors
- [ ] Memory usage stable
- [ ] CPU usage normal
- [ ] Database connections healthy
- [ ] No exceptions logged
- [ ] Customer feedback positive (no complaints)

---

### **Rollback (If Needed)**

**If issues detected, rollback is INSTANT:**

```powershell
# ROLLBACK TO GREEN (< 1 second)

# Step 1: Remove port 443 from Blue
Remove-WebBinding -Name "CapAndCollarSupplyCo-Blue" `
                  -Protocol https `
                  -Port 443

# Step 2: Add port 443 back to Green
New-WebBinding -Name "CapAndCollarSupplyCo" `
               -Protocol https `
               -Port 443 `
               -HostHeader "capandcollarsupplyco.com"

# Step 3: Bind SSL certificate
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My" | 
        Where-Object { $_.Subject -like "*capandcollarsupplyco.com*" }
$binding = Get-WebBinding -Name "CapAndCollarSupplyCo" -Protocol https -Port 443
$binding.AddSslCertificate($cert.Thumbprint, "My")

# DONE! Green is serving again
# Total time: < 1 second
# Customer impact: MINIMAL (< 2 seconds perceived)
```

---

### **Success Confirmation**

**Upgrade is successful when:**

- [?] Blue site serving on port 443 for 30+ minutes
- [?] No errors in event log
- [?] Traffic patterns normal
- [?] Response times acceptable
- [?] All functionality tested and working
- [?] Customer feedback positive
- [?] Monitoring shows stability

**Then safe to decomission Green:**

```powershell
# After 24-48 hours of Blue running successfully

# Stop Green
Stop-WebSite -Name "CapAndCollarSupplyCo"
Stop-WebAppPool -Name "CapAndCollar"

# Keep files for a week (safety)
# Then delete
Remove-WebSite -Name "CapAndCollarSupplyCo" -Confirm
Remove-WebAppPool -Name "CapAndCollar" -Confirm
Remove-Item "C:\inetpub\capandcollar" -Recurse -Confirm
```

---

## ?? Quick Reference Commands

### **Create Blue Environment:**
```powershell
# Complete blue setup in one script
.\Scripts\Migration\Create-BlueEnvironment.ps1
```

### **Test Blue:**
```powershell
# Comprehensive blue testing
.\Scripts\Migration\Test-BlueEnvironment.ps1
```

### **Switch to Blue:**
```powershell
# Atomic traffic switch
.\Scripts\Migration\Switch-To-Blue.ps1
```

### **Rollback to Green:**
```powershell
# Instant rollback
.\Scripts\Migration\Rollback-To-Green.ps1
```

---

## ?? Success Metrics

### **Target SLA:**
- **Downtime:** 0 seconds (customer-perceived)
- **Switch Duration:** < 1 second (actual)
- **Error Rate:** 0% (no errors during transition)
- **Performance Impact:** None (same response times)
- **Data Loss:** 0 (complete data integrity)

### **Your First Upgrade Should Achieve:**
- ? 0 seconds customer-facing downtime
- ? < 2 seconds actual transition (including IIS recycle)
- ? 0 errors logged
- ? 100% data preserved
- ? All functionality intact
- ? Customer satisfaction maintained

---

## ?? Lessons & Best Practices

### **Do's:**
- ? Backup EVERYTHING before starting
- ? Test on staging first
- ? Monitor Blue extensively before switch
- ? Have rollback plan ready
- ? Switch during low-traffic period (even if zero-downtime)
- ? Keep Green running for 24-48 hours after switch
- ? Communicate with stakeholders
- ? Document everything

### **Don'ts:**
- ? Switch without testing Blue first
- ? Delete Green immediately
- ? Switch during peak traffic (first time)
- ? Skip monitoring after switch
- ? Forget to backup database
- ? Panic if issues occur (rollback is instant)

---

## ?? Pro Tips

### **1. Practice First**
Run through entire blue-green process on test environment first.

### **2. Automate**
Create PowerShell scripts for each step (create-blue, test-blue, switch, rollback).

### **3. Monitor Everything**
Set up alerts for errors, response time spikes, high memory usage.

### **4. Communicate**
Let your team know upgrade is happening (even if customers won't notice).

### **5. Time It**
Measure how long each step takes. First time might be slow, future upgrades will be faster.

### **6. Document**
Write down what worked, what didn't, lessons learned.

---

## ?? Future State

**After your first successful zero-downtime upgrade:**

### **You'll have:**
- ? Confidence to upgrade anytime
- ? Proven process that works
- ? Fast rollback capability
- ? No customer impact from upgrades
- ? Competitive advantage (can push updates fast)

### **Future upgrades will be:**
- ? Faster (10-15 minutes total)
- ?? Easier (automated scripts)
- ?? Stress-free (proven process)
- ?? More frequent (weekly if needed)

---

## ?? Additional Resources

**Related Documentation:**
- **INSTALLER-UPGRADE-GUIDE.md** - Using installer for upgrades
- **MIGRATION-GUIDE.md** - Database migration process
- **QUICK-REFERENCE.md** - Daily commands
- **Test-Migration.ps1** - Automated testing

**Microsoft Resources:**
- [IIS Web Farms](https://learn.microsoft.com/iis/web-hosting/scenario-build-a-web-farm-with-iis-servers)
- [Blue-Green Deployments](https://martinfowler.com/bliki/BlueGreenDeployment.html)
- [Zero-Downtime Deployments](https://learn.microsoft.com/azure/architecture/patterns/deployment-stamps)

---

**Created:** 2025-11-09 01:28 AM (Catalyst Autonomous Work Session)  
**By:** Catalyst AI (working autonomously overnight)  
**Purpose:** Enable zero-downtime production upgrades  
**Status:** Production-Ready Strategy ?

---

*"Downtime is expensive. With blue-green deployment, you'll never experience it again."* ??

**Deploy with confidence!** ??
