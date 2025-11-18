<#
.SYNOPSIS
    Diagnostic wrapper for the installer with detailed logging

.DESCRIPTION
    Runs the installer and captures everything with detailed diagnostics
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallerPath = ".\EcommerceStarter.Installer.exe"
)

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DIAGNOSTIC MODE - Detailed Logging" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Environment diagnostics
Write-Host "[DIAGNOSTICS] Environment Information:" -ForegroundColor Yellow
Write-Host "  Current Directory: $(Get-Location)" -ForegroundColor White
Write-Host "  User: $env:USERNAME" -ForegroundColor White
Write-Host "  PowerShell Version: $($PSVersionTable.PSVersion)" -ForegroundColor White
Write-Host "  .NET Runtime: $(dotnet --version 2>$null)" -ForegroundColor White
Write-Host "  OS: $(Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty Caption)" -ForegroundColor White
Write-Host ""

# Check installer
Write-Host "[DIAGNOSTICS] Installer Check:" -ForegroundColor Yellow
if (!(Test-Path $InstallerPath)) {
    Write-Host "  ERROR: Installer not found at: $InstallerPath" -ForegroundColor Red
    exit 1
}

$installerFile = Get-Item $InstallerPath
Write-Host "  Found: $($installerFile.Name)" -ForegroundColor Green
Write-Host "  Size: $('{0:N2}' -f ($installerFile.Length/1MB)) MB" -ForegroundColor Green
Write-Host "  Created: $($installerFile.CreationTime)" -ForegroundColor Green
Write-Host ""

# Create comprehensive log file
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
$logFile = "diagnostic_run_$timestamp.log"
$consoleLogFile = "console_output_$timestamp.log"

Write-Host "[DIAGNOSTICS] Creating log files:" -ForegroundColor Yellow
Write-Host "  Main Log: $logFile" -ForegroundColor White
Write-Host "  Console Log: $consoleLogFile" -ForegroundColor White
Write-Host ""

# Start actual installer
Write-Host "[DIAGNOSTICS] Starting installer process..." -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date
Add-Content -Path $logFile -Value "=== INSTALLER RUN START ==="
Add-Content -Path $logFile -Value "Time: $startTime"
Add-Content -Path $logFile -Value "Directory: $(Get-Location)"
Add-Content -Path $logFile -Value ""

try {
    # Run installer and capture output line by line
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = (Resolve-Path $InstallerPath).Path
    $processInfo.UseShellExecute = $false
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.CreateNoWindow = $true
    
    Write-Host "[PROCESS] Starting process..." -ForegroundColor Cyan
    $process = [System.Diagnostics.Process]::Start($processInfo)
    
    Write-Host "[PROCESS] Process ID: $($process.Id)" -ForegroundColor Green
    Write-Host "[PROCESS] Waiting for completion..." -ForegroundColor Cyan
    Write-Host ""
    
    # Capture output
    $output = $process.StandardOutput.ReadToEnd()
    $error = $process.StandardError.ReadToEnd()
    
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    
    Write-Host ""
    Write-Host "[PROCESS] Process exited with code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
    
    # Log everything
    if ($output) {
        Write-Host ""
        Write-Host "=== STANDARD OUTPUT ===" -ForegroundColor Cyan
        Write-Host $output
        Add-Content -Path $logFile -Value "=== STANDARD OUTPUT ==="
        Add-Content -Path $logFile -Value $output
    }
    
    if ($error) {
        Write-Host ""
        Write-Host "=== STANDARD ERROR ===" -ForegroundColor Red
        Write-Host $error
        Add-Content -Path $logFile -Value "=== STANDARD ERROR ==="
        Add-Content -Path $logFile -Value $error
    }
}
catch {
    Write-Host ""
    Write-Host "[ERROR] Exception: $_" -ForegroundColor Red
    Write-Host "[ERROR] Type: $($_.Exception.GetType().Name)" -ForegroundColor Red
    Add-Content -Path $logFile -Value "EXCEPTION: $_"
}

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[DIAGNOSTICS] Run completed" -ForegroundColor Yellow
Write-Host "  Duration: $($duration.TotalSeconds) seconds" -ForegroundColor White
Write-Host "  Log saved to: $logFile" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Display log contents
Write-Host "Log file contents:" -ForegroundColor Cyan
Write-Host "----------------------------------------"
Get-Content $logFile
Write-Host "----------------------------------------"

# Check for any upgrade logs created by the installer
Write-Host ""
Write-Host "[DIAGNOSTICS] Checking for upgrade logs created by installer..." -ForegroundColor Yellow
$upgradeLogs = Get-ChildItem -Filter "upgrade_*.log" -ErrorAction SilentlyContinue
if ($upgradeLogs) {
    Write-Host "Found $(@($upgradeLogs).Count) upgrade log(s):" -ForegroundColor Green
    foreach ($log in $upgradeLogs) {
        Write-Host "  - $($log.Name) ($('{0:N0}' -f $log.Length) bytes)" -ForegroundColor Green
        Write-Host "    Contents:" -ForegroundColor White
        Get-Content $log | ForEach-Object { Write-Host "      $_" }
    }
} else {
    Write-Host "No upgrade logs found" -ForegroundColor Yellow
}
