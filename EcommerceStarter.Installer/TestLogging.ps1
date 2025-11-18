<#
.SYNOPSIS
    Diagnostic test for logging functionality

.DESCRIPTION
    This script tests if basic file writing works in the current directory
#>

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$testLog = "test_logging_$timestamp.log"

Write-Host "Testing logging in: $(Get-Location)" -ForegroundColor Cyan
Write-Host "Test log file: $testLog" -ForegroundColor Yellow
Write-Host ""

# Test 1: Simple file write
Write-Host "Test 1: Simple file write..." -ForegroundColor Yellow
try {
    "Test entry 1" | Out-File -FilePath $testLog
    Write-Host "✓ File created and written" -ForegroundColor Green
    Get-Content $testLog
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# Test 2: File append
Write-Host ""
Write-Host "Test 2: File append..." -ForegroundColor Yellow
try {
    "Test entry 2" | Add-Content -Path $testLog
    Write-Host "✓ File appended" -ForegroundColor Green
    Get-Content $testLog
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# Test 3: StreamWriter
Write-Host ""
Write-Host "Test 3: StreamWriter with immediate flush..." -ForegroundColor Yellow
try {
    $fs = [System.IO.FileStream]::new($testLog, [System.IO.FileMode]::Append, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)
    $writer = [System.IO.StreamWriter]::new($fs)
    $writer.WriteLine("Test entry 3 from StreamWriter")
    $writer.Flush()
    $writer.Dispose()
    Write-Host "✓ StreamWriter worked" -ForegroundColor Green
    Get-Content $testLog
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# Test 4: Check file properties
Write-Host ""
Write-Host "Test 4: File properties..." -ForegroundColor Yellow
if (Test-Path $testLog) {
    $file = Get-Item $testLog
    Write-Host "✓ File exists" -ForegroundColor Green
    Write-Host "  Size: $($file.Length) bytes" -ForegroundColor Green
    Write-Host "  Created: $($file.CreationTime)" -ForegroundColor Green
    Write-Host "  Modified: $($file.LastWriteTime)" -ForegroundColor Green
} else {
    Write-Host "✗ File does not exist" -ForegroundColor Red
}

Write-Host ""
Write-Host "All tests completed. File contents:" -ForegroundColor Cyan
Write-Host "----------------------------------------"
Get-Content $testLog
Write-Host "----------------------------------------"
