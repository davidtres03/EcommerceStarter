# Launch installer in debug mode for testing
param(
    [switch]$MockExisting,  # Simulate existing installation for testing
    [switch]$NoClean        # Don't clean previous builds
)

Write-Host "?? EcommerceStarter Installer - Debug Launcher" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Set mock environment variables if requested
if ($MockExisting) {
    Write-Host "?? MOCK MODE: Simulating existing installation" -ForegroundColor Yellow
    Write-Host "   Path: C:\inetpub\EcommerceStarter" -ForegroundColor Gray
    Write-Host "   Date: 7 days ago" -ForegroundColor Gray
    Write-Host "   Version: 1.0.0" -ForegroundColor Gray
    Write-Host ""
    
    $env:INSTALLER_MOCK_EXISTING = "true"
    $env:INSTALLER_MOCK_PATH = "C:\inetpub\EcommerceStarter"
    $env:INSTALLER_MOCK_DATE = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss")
    $env:INSTALLER_MOCK_VERSION = "1.0.0"
}

# Navigate to installer directory
$installerPath = Join-Path $PSScriptRoot "..\EcommerceStarter.Installer"
Push-Location $installerPath

try {
    # Clean previous build unless -NoClean specified
    if (-not $NoClean) {
        Write-Host "?? Cleaning previous build..." -ForegroundColor Yellow
        dotnet clean --configuration Debug --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed"
        }
    }
    
    # Build in Debug mode
    Write-Host "?? Building installer (Debug mode)..." -ForegroundColor Yellow
    dotnet build --configuration Debug --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    
    # Find the built executable
    $exePath = "bin\Debug\net8.0-windows\EcommerceStarter.Installer.exe"
    
    if (-not (Test-Path $exePath)) {
        throw "Executable not found at: $exePath"
    }
    
    # Launch the installer
    Write-Host "?? Launching installer..." -ForegroundColor Cyan
    Write-Host "   Executable: $exePath" -ForegroundColor Gray
    
    if ($MockExisting) {
        Write-Host "   Mode: RECONFIGURATION TEST" -ForegroundColor Yellow
    } else {
        Write-Host "   Mode: FRESH INSTALL" -ForegroundColor Green
    }
    
    Write-Host "   Debug Mode: ENABLED (no real changes)" -ForegroundColor Magenta
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Start the installer with --debug flag and wait for it to close
    $process = Start-Process -FilePath $exePath -ArgumentList "--debug" -Verb RunAs -PassThru -Wait
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "?? Installer closed (Exit Code: $($process.ExitCode))" -ForegroundColor Cyan
    
    # Clear mock environment variables
    if ($MockExisting) {
        Remove-Item Env:\INSTALLER_MOCK_EXISTING -ErrorAction SilentlyContinue
        Remove-Item Env:\INSTALLER_MOCK_PATH -ErrorAction SilentlyContinue
        Remove-Item Env:\INSTALLER_MOCK_DATE -ErrorAction SilentlyContinue
        Remove-Item Env:\INSTALLER_MOCK_VERSION -ErrorAction SilentlyContinue
    }
}
catch {
    Write-Host "? Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
