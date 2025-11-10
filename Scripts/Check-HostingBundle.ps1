# ASP.NET Core Hosting Bundle Detection Debug Script

Write-Host "=== Checking for ASP.NET Core Hosting Bundle ===" -ForegroundColor Cyan
Write-Host ""

# Check 64-bit registry
Write-Host "Checking 64-bit Registry..." -ForegroundColor Yellow
$uninstall64 = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
$found64 = $false

Get-ChildItem $uninstall64 -ErrorAction SilentlyContinue | ForEach-Object {
    $displayName = (Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).DisplayName
    if ($displayName -like "*ASP.NET Core*Hosting*") {
        Write-Host "  [FOUND] $displayName" -ForegroundColor Green
        Write-Host "  Registry Key: $($_.PSChildName)" -ForegroundColor Gray
        $found64 = $true
    }
}

if (-not $found64) {
    Write-Host "  [NOT FOUND] in 64-bit registry" -ForegroundColor Red
}

Write-Host ""

# Check 32-bit registry
Write-Host "Checking 32-bit Registry..." -ForegroundColor Yellow
$uninstall32 = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
$found32 = $false

Get-ChildItem $uninstall32 -ErrorAction SilentlyContinue | ForEach-Object {
    $displayName = (Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).DisplayName
    if ($displayName -like "*ASP.NET Core*Hosting*") {
        Write-Host "  [FOUND] $displayName" -ForegroundColor Green
        Write-Host "  Registry Key: $($_.PSChildName)" -ForegroundColor Gray
        $found32 = $true
    }
}

if (-not $found32) {
    Write-Host "  [NOT FOUND] in 32-bit registry" -ForegroundColor Red
}

Write-Host ""

# Check for IIS module file
Write-Host "Checking for IIS Module..." -ForegroundColor Yellow
$modulePath = "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (Test-Path $modulePath) {
    Write-Host "  [FOUND] $modulePath" -ForegroundColor Green
    $fileInfo = Get-Item $modulePath
    Write-Host "  Version: $($fileInfo.VersionInfo.FileVersion)" -ForegroundColor Gray
    Write-Host "  Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "  [NOT FOUND] $modulePath" -ForegroundColor Red
}

Write-Host ""

# Check for dotnet hosting bundle via dotnet command
Write-Host "Checking via 'dotnet --list-runtimes'..." -ForegroundColor Yellow
try {
    $runtimes = dotnet --list-runtimes 2>$null
    $aspNetCore = $runtimes | Where-Object { $_ -like "*Microsoft.AspNetCore.App*" }
    
    if ($aspNetCore) {
        Write-Host "  [FOUND] ASP.NET Core Runtimes:" -ForegroundColor Green
        $aspNetCore | ForEach-Object {
            Write-Host "    $_" -ForegroundColor Gray
        }
    } else {
        Write-Host "  [NOT FOUND] No ASP.NET Core runtimes" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERROR] Could not run 'dotnet' command" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Detection Complete ===" -ForegroundColor Cyan

# Overall result
if ($found64 -or $found32 -or (Test-Path $modulePath)) {
    Write-Host ""
    Write-Host "RESULT: ASP.NET Core Hosting Bundle IS INSTALLED" -ForegroundColor Green -BackgroundColor Black
} else {
    Write-Host ""
    Write-Host "RESULT: ASP.NET Core Hosting Bundle NOT DETECTED" -ForegroundColor Red -BackgroundColor Black
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
