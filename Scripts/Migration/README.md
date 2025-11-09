# ?? Database Migration Testing Suite

**Automated testing and validation for production database ? EcommerceStarter migration**

---

## ?? What's Included

```
Scripts/Migration/
??? Test-Migration.ps1      # Automated migration testing
??? MIGRATION-GUIDE.md      # Complete migration documentation
??? QUICK-REFERENCE.md      # Daily workflow commands
??? README.md              # This file
```

---

## ?? Quick Start

### 1. Run Your First Migration Test

```powershell
# Navigate to migration scripts
cd C:\Dev\Websites\Scripts\Migration

# Run automated test
.\Test-Migration.ps1
```

**What happens:**
1. ? Backs up `CapAndCollarSupplyCo` database
2. ? Restores to `EcommerceStarter_Test`
3. ? Verifies data integrity
4. ? Tests all tables
5. ? Generates connection string
6. ? Creates test report

**Time:** ~2-5 minutes

---

## ?? Documentation

### For Your First Time
Read: **MIGRATION-GUIDE.md**
- Complete migration process
- Troubleshooting steps
- Success criteria
- Deployment process

### For Daily Development
Read: **QUICK-REFERENCE.md**
- Quick commands
- Daily workflow
- Common issues
- Emergency recovery

---

## ?? Common Use Cases

### Use Case 1: "I want to test a new feature"

```powershell
# Create test database
.\Test-Migration.ps1

# Update connection string in appsettings.Development.json
# (Script outputs the correct connection string)

# Run your app
cd C:\Dev\Websites\EcommerceStarter
dotnet run

# Test your feature at https://localhost:7004/
```

### Use Case 2: "I want to verify production compatibility"

```powershell
# Test with latest production backup
.\Test-Migration.ps1

# Review test results
# Check: C:\Temp\DatabaseBackups\MigrationTest_*.json
```

### Use Case 3: "I need a quick test database"

```powershell
# Use existing backup (faster)
.\Test-Migration.ps1 -SkipBackup
```

### Use Case 4: "I want to test before deploying"

```powershell
# Full test with cleanup
.\Test-Migration.ps1 -CleanupAfter

# If all tests pass ? safe to deploy
```

---

## ?? Script Parameters

### Test-Migration.ps1

```powershell
.\Test-Migration.ps1 `
    -SourceServer "localhost\SQLEXPRESS" `     # SQL Server instance
    -SourceDatabase "CapAndCollarSupplyCo" `   # Production database
    -TestDatabase "EcommerceStarter_Test" `    # Test database name
    -BackupPath "C:\Temp\DatabaseBackups" `    # Backup storage
    -SkipBackup `                              # Use existing backup
    -CleanupAfter                              # Delete test DB after
```

**Examples:**

```powershell
# Basic (use defaults)
.\Test-Migration.ps1

# Custom test database name
.\Test-Migration.ps1 -TestDatabase "MyTest"

# Use existing backup (faster)
.\Test-Migration.ps1 -SkipBackup

# Different backup location
.\Test-Migration.ps1 -BackupPath "D:\Backups"

# Test and cleanup
.\Test-Migration.ps1 -CleanupAfter
```

---

## ? What Gets Tested

### Automated Verification

1. **Prerequisites**
   - SQL Server connectivity
   - Source database exists
   - Backup directory accessible

2. **Backup & Restore**
   - Database backup successful
   - Restore completes without errors
   - Files created in correct location

3. **Data Integrity**
   - Connection to test database
   - Branding/Settings preserved
   - Products count correct
   - Users/Identity data intact
   - Orders preserved
   - Admin accounts functional

4. **Schema Validation**
   - Settings table structure
   - All required tables present
   - Foreign keys intact

---

## ?? Test Results

### Output Example

```
???????????????????????????????????????????????????????
  PRODUCTION DATABASE MIGRATION TEST
???????????????????????????????????????????????????????

?? Verifying Prerequisites
? SQL Server connection successful
? Source database found
? Backup directory ready

?? Creating Database Backup
??  Backing up 'CapAndCollarSupplyCo' to:
    C:\Temp\DatabaseBackups\CapAndCollarSupplyCo_20251109_123456.bak
? Backup created successfully
    Size: 45.23 MB

?? Restoring Backup to Test Database
? Database restored successfully

?? Verifying Data Integrity
? Connection successful
? Branding preserved
    Company: Cap & Collar Supply Co.
    Theme: orange
? Products preserved (127 products)
? Users preserved (15 users)
    Admin users: 2
? Orders preserved (45 orders)
? Settings table structure intact (24 columns)

?? Test Results Summary
Test Database: EcommerceStarter_Test
Source Database: CapAndCollarSupplyCo

? BackupCreated
? DatabaseRestored
? ConnectionSuccessful
? BrandingPreserved
? ProductsPreserved
? UsersPreserved
? OrdersPreserved
? AdminLoginWorks
? SettingsPreserved

Results: 9 / 9 tests passed

?? Use this connection string for testing:
Server=localhost\SQLEXPRESS;Database=EcommerceStarter_Test;...

? MIGRATION TEST PASSED! ?
? Production data is compatible with EcommerceStarter!
```

### Test Report JSON

Saved to: `C:\Temp\DatabaseBackups\MigrationTest_[timestamp].json`

```json
{
  "BackupCreated": true,
  "DatabaseRestored": true,
  "ConnectionSuccessful": true,
  "BrandingPreserved": true,
  "ProductsPreserved": true,
  "UsersPreserved": true,
  "OrdersPreserved": true,
  "AdminLoginWorks": true,
  "SettingsPreserved": true,
  "OverallSuccess": true
}
```

---

## ?? Configuration

### Default Settings

Edit script header to change defaults:

```powershell
# Test-Migration.ps1
param(
    [string]$SourceServer = "localhost\SQLEXPRESS",      # Change if needed
    [string]$SourceDatabase = "CapAndCollarSupplyCo",   # Your production DB
    [string]$TestDatabase = "EcommerceStarter_Test",    # Test DB name
    [string]$BackupPath = "C:\Temp\DatabaseBackups",    # Backup location
    # ...
)
```

### Connection String Template

After test completes, use this in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EcommerceStarter_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

---

## ?? Troubleshooting

### Issue: "SQL Server connection failed"

**Check:**
```powershell
# Is SQL Server running?
Get-Service -Name "MSSQL*"

# Can you connect?
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

**Fix:**
```powershell
# Start SQL Server
Start-Service "MSSQL`$SQLEXPRESS"
```

### Issue: "Invoke-Sqlcmd not found"

**Fix:**
```powershell
# Install SqlServer module
Install-Module -Name SqlServer -Scope CurrentUser
Import-Module SqlServer
```

### Issue: "Access Denied"

**Fix:**
- Run PowerShell as **Administrator**
- Verify SQL Server permissions (need `sysadmin` or `db_owner`)

### Issue: "Backup takes too long"

**Optimize:**
```powershell
# Use existing backup
.\Test-Migration.ps1 -SkipBackup

# Or use differential backup (future enhancement)
```

---

## ?? File Structure

```
C:\
??? Dev\
?   ??? Websites\
?       ??? EcommerceStarter\
?       ?   ??? appsettings.Development.json  ? Update this
?       ?   ??? EcommerceStarter.csproj
?       ??? Scripts\
?           ??? Migration\
?               ??? Test-Migration.ps1         ? Run this
?               ??? MIGRATION-GUIDE.md
?               ??? QUICK-REFERENCE.md
?               ??? README.md
??? Temp\
    ??? DatabaseBackups\
        ??? CapAndCollarSupplyCo_*.bak        ? Backups here
        ??? MigrationTest_*.json               ? Test reports here
```

---

## ?? Best Practices

### Daily Development

1. **Morning:** Create fresh test database
   ```powershell
   .\Test-Migration.ps1
   ```

2. **During Day:** Develop on test database
   - Never connect to production directly
   - Test features thoroughly
   - Commit changes frequently

3. **End of Day:** Clean up (optional)
   ```sql
   DROP DATABASE [EcommerceStarter_Test];
   ```

### Weekly

1. **Friday:** Test with latest production backup
   ```powershell
   .\Test-Migration.ps1 -SkipBackup:$false
   ```

2. **Review:** Check test results
3. **Deploy:** If tests pass, deploy to production

### Before Deployment

1. **Backup:** Fresh production backup
2. **Test:** Full migration test
3. **Verify:** All tests pass
4. **Deploy:** With confidence!

---

## ?? Next Steps

### After First Successful Test

1. ? Read **MIGRATION-GUIDE.md** for complete process
2. ? Read **QUICK-REFERENCE.md** for daily commands
3. ? Set up automated daily backups
4. ? Create branch strategy (main vs production)
5. ? Document your specific workflow

### Customize for Your Needs

- Modify default parameters in script
- Add custom validation tests
- Integrate with CI/CD pipeline
- Add notification system (email on test failure)

---

## ?? Additional Resources

### Internal Documentation
- **SESSION_STATE.md** - Your proven test results
- **CHANGELOG.md** - Version history
- **README.md** - Main project documentation

### External Resources
- [SQL Server Backup Best Practices](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/backup-overview-sql-server)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PowerShell SQL Server Module](https://learn.microsoft.com/en-us/sql/powershell/sql-server-powershell)

---

## ? Your Success Story

**From SESSION_STATE.md:**

> **Tested Successfully:**
> - Database: CapAndCollarSupplyCo
> - Branding: Cap & Collar Supply Co. ??
> - Products: All preserved ?
> - Users: All preserved ?
> - Orders: All preserved ?
> - Admin: Existing admins work ?

**This suite codifies your proven process!** ??

Now you can:
- ? Test confidently
- ? Deploy safely
- ? Develop rapidly
- ? Share with open-source community

---

## ?? Contributing

Found an issue? Have an enhancement?

1. Test the issue
2. Document the fix
3. Update scripts
4. Share with the community

---

## ?? Support

**Questions about:**

### Migration Testing
- See: MIGRATION-GUIDE.md
- Run: `Get-Help .\Test-Migration.ps1 -Full`

### Daily Workflow
- See: QUICK-REFERENCE.md

### Deployment
- See: MIGRATION-GUIDE.md ? "Production Deployment Process"

---

**Created:** November 9, 2025  
**By:** David Thomas Resnick & Catalyst AI  
**Version:** 1.0.0  
**Status:** Production-Ready ?

---

*"Test often, deploy confidently, never lose data."* ??

**Happy migrating!** ??
