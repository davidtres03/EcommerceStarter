# Launch uninstaller for testing
param(
    [switch]$Debug  # Launch in debug mode (no real changes)
)

Write-Host "??? EcommerceStarter Uninstaller - Debug Launcher" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to installer directory
$installerPath = Join-Path $PSScriptRoot "..\EcommerceStarter.Installer"
Push-Location $installerPath

try {
    # Build in Debug mode
    Write-Host "?? Building uninstaller..." -ForegroundColor Yellow
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
    
    # Prepare arguments
    $args = @("--uninstall")
    
    if ($Debug) {
        $args += "--debug"
        Write-Host "?? Debug Mode: ENABLED (no real changes will be made)" -ForegroundColor Yellow
    }
    else {
        Write-Host "??  LIVE MODE: Real uninstallation will be performed!" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "??? Launching uninstaller..." -ForegroundColor Cyan
    Write-Host "   Executable: $exePath" -ForegroundColor Gray
    Write-Host "   Arguments: $($args -join ' ')" -ForegroundColor Gray
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Start the uninstaller
    $process = Start-Process -FilePath $exePath -ArgumentList $args -PassThru -Wait
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "??? Uninstaller closed (Exit Code: $($process.ExitCode))" -ForegroundColor Cyan
}
catch {
    Write-Host "? Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
