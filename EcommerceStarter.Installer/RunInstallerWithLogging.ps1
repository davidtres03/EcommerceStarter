<#
.SYNOPSIS
    Run the EcommerceStarter installer with extensive console logging

.DESCRIPTION
    This script runs the installer and captures all output to both console and a log file
    with timestamps and detailed diagnostics.

.EXAMPLE
    .\RunInstallerWithLogging.ps1
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallerPath = ".\EcommerceStarter.Installer.exe"
)

$ErrorActionPreference = "Continue"

# Create timestamp for log file
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "upgrade_verbose_$timestamp.log"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "EcommerceStarter Installer - Verbose Logging Mode" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installer Path: $InstallerPath" -ForegroundColor Yellow
Write-Host "Log File: $logFile" -ForegroundColor Yellow
Write-Host "Current Directory: $(Get-Location)" -ForegroundColor Yellow
Write-Host "Start Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')" -ForegroundColor Yellow
Write-Host ""

# Verify installer exists
if (!(Test-Path $InstallerPath)) {
    Write-Host "ERROR: Installer not found at: $InstallerPath" -ForegroundColor Red
    exit 1
}

Write-Host "Installer file size: $('{0:N2}' -f ((Get-Item $InstallerPath).Length/1MB)) MB" -ForegroundColor Green
Write-Host "Installer last modified: $(Get-Item $InstallerPath | Select-Object -ExpandProperty LastWriteTime)" -ForegroundColor Green
Write-Host ""

# Function to log output
function Log-Message {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $logEntry = "$timestamp [$Level] $Message"
    
    # Write to console
    if ($Level -eq "ERROR") {
        Write-Host $logEntry -ForegroundColor Red
    } elseif ($Level -eq "SUCCESS") {
        Write-Host $logEntry -ForegroundColor Green
    } elseif ($Level -eq "WARNING") {
        Write-Host $logEntry -ForegroundColor Yellow
    } else {
        Write-Host $logEntry -ForegroundColor Cyan
    }
    
    # Write to file
    Add-Content -Path $logFile -Value $logEntry
}

Log-Message "Starting installer process..." "INFO"
Log-Message "PowerShell Version: $($PSVersionTable.PSVersion)" "INFO"
Log-Message "OS Version: $(Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty Caption)" "INFO"

# Run the installer and capture all output
Log-Message "Launching: $InstallerPath" "INFO"
Log-Message "==================== INSTALLER OUTPUT BEGINS ====================" "INFO"

try {
    # Run the installer and stream output
    & $InstallerPath 2>&1 | ForEach-Object {
        $line = $_
        Write-Host $line -ForegroundColor White
        Add-Content -Path $logFile -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff') [INSTALLER] $line"
    }
    
    $exitCode = $LASTEXITCODE
    Log-Message "==================== INSTALLER OUTPUT ENDS ====================" "INFO"
    Log-Message "Installer exited with code: $exitCode" $(if ($exitCode -eq 0) { "SUCCESS" } else { "ERROR" })
}
catch {
    Log-Message "Exception occurred: $_" "ERROR"
    Log-Message "Exception Type: $($_.Exception.GetType().Name)" "ERROR"
    Log-Message "Stack Trace: $($_.ScriptStackTrace)" "ERROR"
}

Log-Message "End Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')" "INFO"
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Log file saved to: $(Convert-Path $logFile)" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

# Display the log file
Write-Host ""
Write-Host "Displaying log file contents:" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan
Get-Content $logFile
Write-Host "----------------------------------------" -ForegroundColor Cyan
