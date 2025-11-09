# ?? Quick Reference: Development ? Production Workflow

**Daily development workflow for keeping EcommerceStarter open-source while actively developing Cap & Collar production.**

---

## ? Quick Commands

### Test Migration (Full Automated)
```powershell
cd C:\Dev\Websites\Scripts\Migration
.\Test-Migration.ps1
```

### Test Migration (Use Existing Backup)
```powershell
.\Test-Migration.ps1 -SkipBackup
```

### Test Migration (Custom Settings)
```powershell
.\Test-Migration.ps1 `
    -SourceDatabase "CapAndCollarSupplyCo" `
    -TestDatabase "MyTest" `
    -BackupPath "D:\Backups"
```

### Manual Backup
```powershell
$date = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = "C:\Temp\DatabaseBackups\CapAndCollar_$date.bak"
Invoke-Sqlcmd -ServerInstance "localhost\SQLEXPRESS" -Query "BACKUP DATABASE [CapAndCollarSupplyCo] TO DISK = N'$backup' WITH COMPRESSION"
```

### Run with Test Database
```bash
cd C:\Dev\Websites\EcommerceStarter

# Make sure appsettings.Development.json points to test DB
dotnet run

# Navigate to: https://localhost:7004/
```

---

## ?? Daily Development Flow

### Morning Setup (Start of Development)

1. **Create fresh test database**
   ```powershell
   .\Test-Migration.ps1
   ```

2. **Verify connection string**
   ```json
   // appsettings.Development.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EcommerceStarter_Test;..."
     }
   }
   ```

3. **Start development server**
   ```bash
   dotnet watch run
   ```

### During Development

**? DO:**
- Test on `EcommerceStarter_Test` database
- Commit changes frequently
- Test each feature thoroughly
- Keep production data in mind

**? DON'T:**
- Connect to production database directly
- Test payment processing on production
- Make schema changes without migrations
- Hardcode Cap & Collar specific values

### End of Day (After Development)

1. **Commit changes**
   ```bash
   git add .
   git commit -m "feat: Add new feature"
   git push origin main
   ```

2. **Optional: Clean up test database**
   ```sql
   DROP DATABASE [EcommerceStarter_Test];
   ```

---

## ?? Weekly: Sync with Production

### Every Friday (or before major deployments)

1. **Backup latest production**
   ```powershell
   .\Test-Migration.ps1 -SourceDatabase "CapAndCollarSupplyCo"
   ```

2. **Test with latest EcommerceStarter code**
   ```bash
   dotnet run
   # Visit https://localhost:7004/
   # Verify everything works
   ```

3. **If tests pass ? Deploy to production**
   ```bash
   # Pull latest code on production server
   git pull origin production
   
   # Publish
   dotnet publish -c Release -o C:\inetpub\capandcollar
   
   # Restart IIS
   Restart-WebAppPool -Name "CapAndCollar"
   ```

---

## ?? Feature Development Checklist

### Before Starting Feature

- [ ] Production backup exists (< 1 week old)
- [ ] Test database created
- [ ] Connection string points to test DB
- [ ] Development server running
- [ ] Git branch created (optional: `feature/my-feature`)

### During Feature Development

- [ ] Code changes work on test data
- [ ] No production-specific hardcoding
- [ ] Database changes use EF migrations
- [ ] Tests pass (if you have them)
- [ ] No breaking changes to existing features

### After Feature Complete

- [ ] Feature tested end-to-end
- [ ] Admin panel still works
- [ ] Checkout flow unaffected
- [ ] Migrations reviewed
- [ ] Committed to Git
- [ ] Merged to main branch

### Before Production Deployment

- [ ] Fresh production backup created
- [ ] Tested on production-like environment
- [ ] Rollback plan ready
- [ ] Deployment window scheduled
- [ ] Stakeholders notified

---

## ?? Troubleshooting Quick Fixes

### Site Won't Load

```bash
# Check connection string
cat appsettings.Development.json

# Test database connection
sqlcmd -S localhost\SQLEXPRESS -d EcommerceStarter_Test -E -Q "SELECT @@VERSION"

# Check for errors
dotnet run --environment Development
```

### Database Errors

```powershell
# Check if test DB exists
Invoke-Sqlcmd -Query "SELECT DB_ID('EcommerceStarter_Test') as DbId"

# Recreate test database
.\Test-Migration.ps1 -SkipBackup:$false
```

### Migration Issues

```bash
# List migrations
dotnet ef migrations list

# Check database schema version
dotnet ef migrations script --idempotent --output schema.sql

# Apply missing migrations
dotnet ef database update
```

### "Admin Not Found" Error

Your installer already handles this (from SESSION_STATE.md)!

But if needed:
```sql
-- Check admin exists
SELECT u.Email, r.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId  
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin';

-- If none, installer will skip creating new admin
-- Your fix: Installer detects existing admins ?
```

### Branding Doesn't Show

```sql
-- Check Settings table
SELECT * FROM Settings;

-- If empty, seed default
INSERT INTO Settings (CompanyName, ThemeColor, PrimaryColor)
VALUES ('EcommerceStarter', 'blue', '#0d6efd');
```

---

## ?? Health Check Commands

### Quick System Check

```powershell
# SQL Server running?
Get-Service -Name "MSSQL*"

# IIS running?
Get-Service -Name "W3SVC"

# Test database exists?
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT name FROM sys.databases WHERE name LIKE 'Ecommerce%'"

# Recent backups?
Get-ChildItem C:\Temp\DatabaseBackups\*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 5
```

### Verify Production Integrity

```sql
-- Product count
SELECT COUNT(*) as Products FROM Products;

-- User count  
SELECT COUNT(*) as Users FROM AspNetUsers;

-- Order count
SELECT COUNT(*) as Orders FROM Orders;

-- Settings configured
SELECT CompanyName, ThemeColor FROM Settings;
```

---

## ?? Branch Strategy (Recommended)

```
main (open-source EcommerceStarter)
  ?
  ?? feature/new-feature
  ?? fix/bug-123
  ?? develop (integration testing)

production (Cap & Collar specific)
  ? (cherry-pick from main)
```

### Commands

```bash
# Start new feature
git checkout -b feature/my-feature main

# Develop and commit
git add .
git commit -m "feat: Add feature"

# Merge to main (open-source)
git checkout main
git merge feature/my-feature
git push origin main

# Cherry-pick to production
git checkout production
git cherry-pick <commit-hash>
git push origin production
```

---

## ?? Production Deployment (Step-by-Step)

### Pre-Deployment

```powershell
# 1. Backup production
.\Test-Migration.ps1 -SourceDatabase "CapAndCollarSupplyCo" -BackupPath "C:\Backups\PreDeployment"

# 2. Test on staging
cd C:\Dev\Websites\EcommerceStarter
dotnet run
# Verify at https://localhost:7004/

# 3. Tag release
git tag -a v1.1.0 -m "Release v1.1.0"
git push origin v1.1.0
```

### Deployment

```powershell
# On production server

# 1. Stop site
Stop-WebSite -Name "CapAndCollarSupplyCo"

# 2. Pull latest code
git pull origin production

# 3. Publish
dotnet publish -c Release -o C:\inetpub\capandcollar

# 4. Update database (if needed)
dotnet ef database update

# 5. Start site
Start-WebSite -Name "CapAndCollarSupplyCo"

# 6. Smoke test
Invoke-WebRequest https://capandcollarsupplyco.com/ -UseBasicParsing
```

### Post-Deployment

```powershell
# 1. Monitor logs
Get-Content C:\inetpub\capandcollar\logs\*.log -Tail 50 -Wait

# 2. Test critical paths
# - Homepage loads
# - Products display
# - Admin login works
# - Checkout flow works

# 3. Monitor for 30 minutes
# Watch for errors, performance issues

# 4. If issues ? Rollback
git reset --hard v1.0.0
dotnet publish -c Release -o C:\inetpub\capandcollar
Restart-WebAppPool -Name "CapAndCollar"
```

---

## ?? Emergency Contacts & Commands

### Site Down - Quick Recovery

```powershell
# Check site status
Get-Website | Where-Object { $_.Name -like "*CapAndCollar*" }

# Restart app pool
Restart-WebAppPool -Name "CapAndCollar"

# Restart website
Restart-WebSite -Name "CapAndCollarSupplyCo"

# Check event log
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 10
```

### Database Issues - Quick Recovery

```powershell
# Find latest backup
$backup = Get-ChildItem C:\Temp\DatabaseBackups\*.bak | 
          Sort-Object LastWriteTime -Descending | 
          Select-Object -First 1

# Restore database
Invoke-Sqlcmd -Query "
ALTER DATABASE [CapAndCollarSupplyCo] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [CapAndCollarSupplyCo] FROM DISK = N'$($backup.FullName)' WITH REPLACE;
ALTER DATABASE [CapAndCollarSupplyCo] SET MULTI_USER;
"
```

---

## ?? Daily Checklist Template

```markdown
## Development Session: [DATE]

### Setup
- [ ] Test database created/verified
- [ ] Connection string correct
- [ ] Development server started
- [ ] Git status clean

### Work Done
- [ ] Feature/fix: _______________
- [ ] Tested locally
- [ ] No breaking changes
- [ ] Documentation updated

### Cleanup
- [ ] Changes committed
- [ ] Pushed to remote
- [ ] Test database dropped (optional)
- [ ] Notes saved

### Issues Encountered
- None / [describe]

### Next Session
- [ ] Continue with: _______________
```

---

## ?? Success Metrics

**Your development process is working when:**

? Can test changes without touching production
? Production data works with EcommerceStarter code
? Features deploy smoothly to production
? Rollback process is tested and reliable
? Open-source code stays clean (no production secrets)
? Development velocity is high (not blocked by testing)

---

**Created:** November 9, 2025  
**By:** David & Catalyst  
**For:** Daily development happiness ??

---

*Keep calm and test on EcommerceStarter_Test! ??*
