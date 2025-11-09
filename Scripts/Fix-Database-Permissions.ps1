#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Fixes SQL Server permissions for IIS Application Pool
    
.DESCRIPTION
    Grants the IIS Application Pool user access to the database
    
.PARAMETER AppPoolName
    The name of the IIS Application Pool (default: MyStore)
    
.PARAMETER DatabaseName
    The name of the database (default: MyStore)
    
.PARAMETER ServerInstance
    The SQL Server instance (default: localhost\SQLEXPRESS)
#>

param(
    [string]$AppPoolName = "MyStore",
    [string]$DatabaseName = "MyStore",
    [string]$ServerInstance = "localhost\SQLEXPRESS"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Fix Database Permissions" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  App Pool: $AppPoolName" -ForegroundColor White
Write-Host "  Database: $DatabaseName" -ForegroundColor White
Write-Host "  Server: $ServerInstance" -ForegroundColor White
Write-Host ""

# The IIS App Pool user
$appPoolUser = "IIS APPPOOL\$AppPoolName"

Write-Host "Creating SQL login and granting permissions..." -ForegroundColor Yellow

# SQL script to create login and grant permissions
$sqlScript = @"
USE [master];
GO

-- Create login for IIS App Pool user if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = N'$appPoolUser')
BEGIN
    CREATE LOGIN [$appPoolUser] FROM WINDOWS WITH DEFAULT_DATABASE=[master];
    PRINT 'Login created for $appPoolUser';
END
ELSE
BEGIN
    PRINT 'Login already exists for $appPoolUser';
END
GO

USE [$DatabaseName];
GO

-- Create user in database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'$appPoolUser')
BEGIN
    CREATE USER [$appPoolUser] FOR LOGIN [$appPoolUser];
    PRINT 'User created in database $DatabaseName';
END
ELSE
BEGIN
    PRINT 'User already exists in database $DatabaseName';
END
GO

-- Grant db_owner role (full permissions)
ALTER ROLE [db_owner] ADD MEMBER [$appPoolUser];
PRINT 'Granted db_owner role to $appPoolUser';
GO

PRINT 'Permissions configured successfully!';
GO
"@

# Save SQL script to temp file
$tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
$sqlScript | Out-File -FilePath $tempSqlFile -Encoding UTF8

try {
    # Execute SQL script using sqlcmd
    Write-Host "Executing SQL commands..." -ForegroundColor Yellow
    
    $result = sqlcmd -S $ServerInstance -E -i $tempSqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "? Success! Database permissions configured." -ForegroundColor Green
        Write-Host ""
        Write-Host "Output:" -ForegroundColor Cyan
        $result | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        Write-Host ""
        Write-Host "The IIS Application Pool '$AppPoolName' now has access to database '$DatabaseName'." -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Restart the IIS Application Pool" -ForegroundColor White
        Write-Host "  2. Try accessing http://localhost/$AppPoolName again" -ForegroundColor White
        Write-Host ""
        
        # Offer to restart the app pool
        $restart = Read-Host "Do you want to restart the '$AppPoolName' application pool now? (y/n)"
        if ($restart -eq 'y' -or $restart -eq 'Y') {
            Import-Module WebAdministration
            Restart-WebAppPool -Name $AppPoolName
            Write-Host "? Application pool restarted" -ForegroundColor Green
        }
    }
    else {
        Write-Host ""
        Write-Host "? Error executing SQL commands" -ForegroundColor Red
        Write-Host ""
        Write-Host "Error output:" -ForegroundColor Red
        $result | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        Write-Host ""
        Write-Host "Troubleshooting:" -ForegroundColor Yellow
        Write-Host "  - Make sure SQL Server is running" -ForegroundColor White
        Write-Host "  - Verify the server instance name: $ServerInstance" -ForegroundColor White
        Write-Host "  - Check if database '$DatabaseName' exists" -ForegroundColor White
        Write-Host "  - Ensure you have sysadmin rights on SQL Server" -ForegroundColor White
    }
}
catch {
    Write-Host ""
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Message -like "*sqlcmd*not recognized*") {
        Write-Host "sqlcmd is not installed or not in PATH" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Alternative: Run this SQL manually in SSMS:" -ForegroundColor Cyan
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host $sqlScript -ForegroundColor White
        Write-Host "----------------------------------------" -ForegroundColor Gray
    }
}
finally {
    # Clean up temp file
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
