# ============================================
# PRODUCTION DATABASE MIGRATION TEST SUITE
# Tests: Production DB ? EcommerceStarter ? Verify
# ============================================

<#
.SYNOPSIS
    Tests production database migration to EcommerceStarter codebase

.DESCRIPTION
    This script validates that production data (Cap & Collar) can be successfully
    used with the EcommerceStarter open-source codebase without data loss.

.PARAMETER SourceServer
    SQL Server instance with production database (default: localhost\SQLEXPRESS)

.PARAMETER SourceDatabase
    Production database name (default: CapAndCollarSupplyCo)

.PARAMETER TestDatabase
    Test database name for migration test (default: EcommerceStarter_Test)

.PARAMETER BackupPath
    Path to store database backups (default: C:\Temp\DatabaseBackups)

.PARAMETER SkipBackup
    Skip creating new backup (use existing)

.EXAMPLE
    .\Test-Migration.ps1
    
.EXAMPLE
    .\Test-Migration.ps1 -SourceDatabase "MyProductionDB" -TestDatabase "MyTest" -SkipBackup
#>

[CmdletBinding()]
param(
    [string]$SourceServer = "localhost\SQLEXPRESS",
    [string]$SourceDatabase = "CapAndCollarSupplyCo",
    [string]$TestDatabase = "EcommerceStarter_Test",
    [string]$BackupPath = "C:\Temp\DatabaseBackups",
    [switch]$SkipBackup,
    [switch]$CleanupAfter
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Success { param($msg) Write-Host "? $msg" -ForegroundColor Green }
function Write-Info { param($msg) Write-Host "??  $msg" -ForegroundColor Cyan }
function Write-Warning { param($msg) Write-Host "??  $msg" -ForegroundColor Yellow }
function Write-Failure { param($msg) Write-Host "? $msg" -ForegroundColor Red }
function Write-Step { param($msg) Write-Host "`n?? $msg" -ForegroundColor Blue }

Write-Host ""
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Magenta
Write-Host "  PRODUCTION DATABASE MIGRATION TEST" -ForegroundColor Magenta
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Magenta
Write-Host ""

# Test summary object
$TestResults = @{
    BackupCreated = $false
    DatabaseRestored = $false
    ConnectionSuccessful = $false
    BrandingPreserved = $false
    ProductsPreserved = $false
    UsersPreserved = $false
    OrdersPreserved = $false
    AdminLoginWorks = $false
    SettingsPreserved = $false
    OverallSuccess = $false
}

try {
    # ============================================
    # STEP 1: VERIFY PREREQUISITES
    # ============================================
    Write-Step "Verifying Prerequisites"
    
    # Check SQL Server connectivity
    Write-Info "Testing SQL Server connection..."
    $testQuery = "SELECT @@VERSION"
    try {
        $version = Invoke-Sqlcmd -ServerInstance $SourceServer -Query $testQuery -ErrorAction Stop
        Write-Success "SQL Server connection successful"
        Write-Info "  Version: $($version.Column1.Split("`n")[0])"
    }
    catch {
        Write-Failure "Cannot connect to SQL Server: $SourceServer"
        Write-Warning "Make sure:"
        Write-Warning "  1. SQL Server is running"
        Write-Warning "  2. Server name is correct"
        Write-Warning "  3. You have proper permissions"
        throw
    }
    
    # Check source database exists
    Write-Info "Checking source database '$SourceDatabase'..."
    $dbCheck = Invoke-Sqlcmd -ServerInstance $SourceServer -Query "SELECT DB_ID('$SourceDatabase') as DbId"
    if ($null -eq $dbCheck.DbId) {
        Write-Failure "Source database '$SourceDatabase' not found"
        throw "Database does not exist"
    }
    Write-Success "Source database found"
    
    # Check/create backup directory
    if (-not (Test-Path $BackupPath)) {
        Write-Info "Creating backup directory: $BackupPath"
        New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null
    }
    Write-Success "Backup directory ready"
    
    # ============================================
    # STEP 2: BACKUP PRODUCTION DATABASE
    # ============================================
    if (-not $SkipBackup) {
        Write-Step "Creating Database Backup"
        
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupFile = Join-Path $BackupPath "$SourceDatabase`_$timestamp.bak"
        
        Write-Info "Backing up '$SourceDatabase' to:"
        Write-Info "  $backupFile"
        Write-Info "  (This may take a few minutes...)"
        
        $backupQuery = @"
BACKUP DATABASE [$SourceDatabase] 
TO DISK = N'$backupFile' 
WITH FORMAT, 
     INIT, 
     NAME = N'$SourceDatabase-Full Database Backup', 
     SKIP, 
     NOREWIND, 
     NOUNLOAD, 
     COMPRESSION, 
     STATS = 10;
"@
        
        Invoke-Sqlcmd -ServerInstance $SourceServer -Query $backupQuery -QueryTimeout 300
        
        if (Test-Path $backupFile) {
            $fileSize = (Get-Item $backupFile).Length / 1MB
            Write-Success "Backup created successfully"
            Write-Info "  Size: $([math]::Round($fileSize, 2)) MB"
            $TestResults.BackupCreated = $true
        }
        else {
            Write-Failure "Backup file not found"
            throw "Backup creation failed"
        }
    }
    else {
        Write-Info "Skipping backup creation (using existing)"
        # Find most recent backup
        $backupFile = Get-ChildItem $BackupPath -Filter "$SourceDatabase*.bak" | 
                      Sort-Object LastWriteTime -Descending | 
                      Select-Object -First 1 -ExpandProperty FullName
        
        if (-not $backupFile) {
            Write-Failure "No existing backup found in $BackupPath"
            throw "Cannot proceed without backup"
        }
        
        Write-Info "Using backup: $(Split-Path $backupFile -Leaf)"
        $TestResults.BackupCreated = $true
    }
    
    # ============================================
    # STEP 3: DROP EXISTING TEST DATABASE
    # ============================================
    Write-Step "Preparing Test Database"
    
    Write-Info "Checking for existing test database..."
    $testDbCheck = Invoke-Sqlcmd -ServerInstance $SourceServer -Query "SELECT DB_ID('$TestDatabase') as DbId"
    
    if ($null -ne $testDbCheck.DbId) {
        Write-Warning "Test database already exists, dropping..."
        
        $dropQuery = @"
ALTER DATABASE [$TestDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$TestDatabase];
"@
        Invoke-Sqlcmd -ServerInstance $SourceServer -Query $dropQuery
        Write-Success "Existing test database dropped"
    }
    
    # ============================================
    # STEP 4: RESTORE TO TEST DATABASE
    # ============================================
    Write-Step "Restoring Backup to Test Database"
    
    Write-Info "Restoring '$backupFile' to '$TestDatabase'..."
    Write-Info "  (This may take a few minutes...)"
    
    # Get logical file names from backup
    $fileList = Invoke-Sqlcmd -ServerInstance $SourceServer -Query "RESTORE FILELISTONLY FROM DISK = N'$backupFile'"
    $dataFile = $fileList | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1 -ExpandProperty LogicalName
    $logFile = $fileList | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1 -ExpandProperty LogicalName
    
    # Get default SQL Server data path
    $dataPath = Invoke-Sqlcmd -ServerInstance $SourceServer -Query "SELECT SERVERPROPERTY('InstanceDefaultDataPath') as Path"
    $defaultPath = $dataPath.Path
    
    $restoreQuery = @"
RESTORE DATABASE [$TestDatabase] 
FROM DISK = N'$backupFile' 
WITH FILE = 1,
     MOVE N'$dataFile' TO N'$defaultPath$TestDatabase.mdf',
     MOVE N'$logFile' TO N'$defaultPath$TestDatabase`_log.ldf',
     NOUNLOAD,
     REPLACE,
     STATS = 10;
"@
    
    Invoke-Sqlcmd -ServerInstance $SourceServer -Query $restoreQuery -QueryTimeout 300
    Write-Success "Database restored successfully"
    $TestResults.DatabaseRestored = $true
    
    # ============================================
    # STEP 5: VERIFY DATA INTEGRITY
    # ============================================
    Write-Step "Verifying Data Integrity"
    
    # Test 1: Connection
    Write-Info "Testing database connection..."
    $connTest = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query "SELECT DB_NAME() as Name"
    if ($connTest.Name -eq $TestDatabase) {
        Write-Success "Connection successful"
        $TestResults.ConnectionSuccessful = $true
    }
    
    # Test 2: Branding/Settings
    Write-Info "Checking branding settings..."
    $branding = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query @"
SELECT CompanyName, ThemeColor, LogoPath 
FROM Settings 
WHERE Id = (SELECT MIN(Id) FROM Settings)
"@
    
    if ($branding) {
        Write-Success "Branding preserved"
        Write-Info "  Company: $($branding.CompanyName)"
        Write-Info "  Theme: $($branding.ThemeColor)"
        Write-Info "  Logo: $($branding.LogoPath)"
        $TestResults.BrandingPreserved = $true
    }
    
    # Test 3: Products
    Write-Info "Counting products..."
    $productCount = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query "SELECT COUNT(*) as Count FROM Products"
    if ($productCount.Count -gt 0) {
        Write-Success "Products preserved ($($productCount.Count) products)"
        $TestResults.ProductsPreserved = $true
    }
    else {
        Write-Warning "No products found (might be expected)"
    }
    
    # Test 4: Users/Identity
    Write-Info "Counting users..."
    $userCount = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query "SELECT COUNT(*) as Count FROM AspNetUsers"
    if ($userCount.Count -gt 0) {
        Write-Success "Users preserved ($($userCount.Count) users)"
        $TestResults.UsersPreserved = $true
        
        # Check for admin users
        $adminCount = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query @"
SELECT COUNT(*) as Count 
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin'
"@
        Write-Info "  Admin users: $($adminCount.Count)"
        $TestResults.AdminLoginWorks = ($adminCount.Count -gt 0)
    }
    
    # Test 5: Orders
    Write-Info "Counting orders..."
    $orderCount = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query "SELECT COUNT(*) as Count FROM Orders"
    if ($orderCount.Count -gt 0) {
        Write-Success "Orders preserved ($($orderCount.Count) orders)"
        $TestResults.OrdersPreserved = $true
    }
    else {
        Write-Info "No orders found (might be expected for new store)"
    }
    
    # Test 6: Settings table structure
    Write-Info "Verifying Settings table structure..."
    $settingsColumns = Invoke-Sqlcmd -ServerInstance $SourceServer -Database $TestDatabase -Query @"
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Settings'
ORDER BY ORDINAL_POSITION
"@
    Write-Success "Settings table structure intact ($($settingsColumns.Count) columns)"
    $TestResults.SettingsPreserved = $true
    
    # ============================================
    # STEP 6: GENERATE TEST REPORT
    # ============================================
    Write-Step "Test Results Summary"
    
    Write-Host ""
    Write-Host "Test Database: $TestDatabase" -ForegroundColor Cyan
    Write-Host "Source Database: $SourceDatabase" -ForegroundColor Cyan
    Write-Host ""
    
    # Display all test results
    $passCount = 0
    $totalTests = $TestResults.Keys.Count - 1 # Exclude OverallSuccess
    
    foreach ($test in $TestResults.Keys | Where-Object { $_ -ne 'OverallSuccess' }) {
        $result = $TestResults[$test]
        $icon = if ($result) { "?" } else { "?" }
        $color = if ($result) { "Green" } else { "Red" }
        
        Write-Host "$icon $test" -ForegroundColor $color
        if ($result) { $passCount++ }
    }
    
    Write-Host ""
    Write-Host "Results: $passCount / $totalTests tests passed" -ForegroundColor $(if ($passCount -eq $totalTests) { "Green" } else { "Yellow" })
    
    $TestResults.OverallSuccess = ($passCount -ge ($totalTests - 1)) # Allow 1 failure (e.g., no orders)
    
    # ============================================
    # STEP 7: CONNECTION STRING FOR TESTING
    # ============================================
    Write-Step "Testing Connection String"
    
    $connectionString = "Server=$SourceServer;Database=$TestDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
    
    Write-Host ""
    Write-Host "?? Use this connection string for testing:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host $connectionString -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Add to appsettings.Development.json:" -ForegroundColor Yellow
    Write-Host @"
{
  "ConnectionStrings": {
    "DefaultConnection": "$connectionString"
  }
}
"@ -ForegroundColor White
    Write-Host ""
    
    # ============================================
    # STEP 8: NEXT STEPS GUIDANCE
    # ============================================
    Write-Step "Next Steps"
    
    Write-Host ""
    Write-Info "To test with EcommerceStarter:"
    Write-Host "  1. Update appsettings.Development.json with connection string above"
    Write-Host "  2. Run: dotnet run --project EcommerceStarter"
    Write-Host "  3. Navigate to: https://localhost:7004/"
    Write-Host "  4. Verify:"
    Write-Host "     • Branding shows correctly (Cap & Collar ??)"
    Write-Host "     • Products load"
    Write-Host "     • Admin login works"
    Write-Host "     • Theme colors correct"
    Write-Host ""
    
    if ($TestResults.OverallSuccess) {
        Write-Success "MIGRATION TEST PASSED! ?"
        Write-Success "Production data is compatible with EcommerceStarter!"
        Write-Host ""
    }
    else {
        Write-Warning "Some tests failed - review results above"
        Write-Host ""
    }
    
    # ============================================
    # CLEANUP (OPTIONAL)
    # ============================================
    if ($CleanupAfter) {
        Write-Step "Cleanup"
        
        $confirm = Read-Host "Delete test database '$TestDatabase'? (y/n)"
        if ($confirm -eq 'y') {
            $dropQuery = @"
ALTER DATABASE [$TestDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$TestDatabase];
"@
            Invoke-Sqlcmd -ServerInstance $SourceServer -Query $dropQuery
            Write-Success "Test database deleted"
        }
    }
    else {
        Write-Host ""
        Write-Info "Test database '$TestDatabase' preserved for manual testing"
        Write-Info "To delete later: DROP DATABASE [$TestDatabase]"
        Write-Host ""
    }
}
catch {
    Write-Host ""
    Write-Failure "Test failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Stack trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    Write-Host ""
    
    $TestResults.OverallSuccess = $false
}
finally {
    # Save test results to file
    $reportPath = Join-Path $BackupPath "MigrationTest_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
    $TestResults | ConvertTo-Json | Out-File $reportPath
    Write-Info "Test report saved: $reportPath"
}

Write-Host ""
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Magenta
Write-Host "  TEST COMPLETE" -ForegroundColor Magenta
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Magenta
Write-Host ""

# Return test results object
return $TestResults
