

<#
.SYNOPSIS
    Test admin user creation directly
#>

Write-Host "Testing Admin User Creation" -ForegroundColor Cyan
Write-Host ""

# Test credentials
$adminEmail = "test@example.com"
$adminPassword = "Test@123"
$databaseServer = "localhost\SQLEXPRESS"
$databaseName = "MyStore"

Write-Host "Admin Email: $adminEmail" -ForegroundColor Yellow
Write-Host "Database: $databaseServer\$databaseName" -ForegroundColor Yellow
Write-Host ""

# First, let's use the PasswordHasher from Microsoft.AspNetCore.Identity
# We'll need to load the DLL
$identityDllPath = "C:\Dev\Websites\EcommerceStarter.Installer\bin\Debug\net8.0-windows\Microsoft.AspNetCore.Identity.dll"

if (Test-Path $identityDllPath) {
    Write-Host "Loading Identity DLL..." -ForegroundColor Yellow
    Add-Type -Path $identityDllPath
    
    # Create password hasher
    $hasher = New-Object Microsoft.AspNetCore.Identity.PasswordHasher[object]
    $hashedPassword = $hasher.HashPassword($null, $adminPassword)
    
    Write-Host "Password hashed successfully" -ForegroundColor Green
    Write-Host "Hash length: $($hashedPassword.Length) characters" -ForegroundColor Gray
    Write-Host ""
    
    # Escape for SQL
    $escapedEmail = $adminEmail.Replace("'", "''")
    $escapedHash = $hashedPassword.Replace("'", "''")
    
    # Create SQL script
    $sqlScript = @"
-- Ensure roles exist
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
    PRINT 'Created Admin role'
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Customer')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Customer', 'CUSTOMER', NEWID())
    PRINT 'Created Customer role'
END

-- Check if admin user exists
IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = '$escapedEmail')
BEGIN
    DECLARE @UserId NVARCHAR(450) = CAST(NEWID() AS NVARCHAR(450))
    DECLARE @AdminRoleId NVARCHAR(450)
    
    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin'
    
    -- Insert admin user with hashed password
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount
    )
    VALUES (
        @UserId, 
        '$escapedEmail', 
        UPPER('$escapedEmail'), 
        '$escapedEmail', 
        UPPER('$escapedEmail'),
        1, 
        '$escapedHash', 
        NEWID(), 
        NEWID(),
        0, 0, 1, 0
    )
    
    -- Assign Admin role
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId)
    
    PRINT 'Admin user created successfully'
END
ELSE
BEGIN
    PRINT 'Admin user already exists'
END
"@
    
    # Save to temp file
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $sqlScript | Out-File -FilePath $tempFile -Encoding UTF8
    
    Write-Host "Executing SQL script..." -ForegroundColor Yellow
    
    # Execute
    $result = sqlcmd -S $databaseServer -d $databaseName -E -i $tempFile 2>&1
    
    Write-Host ""
    Write-Host "SQL Output:" -ForegroundColor Cyan
    $result | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    
    # Clean up
    Remove-Item $tempFile -Force
    
    Write-Host ""
    Write-Host "Verifying user was created..." -ForegroundColor Yellow
    $verify = sqlcmd -S $databaseServer -d $databaseName -Q "SELECT Email, UserName, EmailConfirmed, CASE WHEN PasswordHash IS NULL THEN 'NO' ELSE 'YES' END as HasPassword FROM AspNetUsers WHERE Email = '$escapedEmail'" -h -1
    
    Write-Host ""
    Write-Host "Verification:" -ForegroundColor Cyan
    $verify | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
    
} else {
    Write-Host "ERROR: Identity DLL not found at: $identityDllPath" -ForegroundColor Red
    Write-Host "Build the installer first: dotnet build EcommerceStarter.Installer" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
