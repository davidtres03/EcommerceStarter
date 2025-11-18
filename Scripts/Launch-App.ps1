# Launch EcommerceStarter application for development testing
param(
    [switch]$Release  # Use Release build instead of Debug
)

Write-Host "?? EcommerceStarter Application - Debug Launcher" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$configuration = if ($Release) { "Release" } else { "Debug" }

# Navigate to project directory
$projectPath = Join-Path $PSScriptRoot "..\EcommerceStarter"
Push-Location $projectPath

try {
    # Build the application
    Write-Host "?? Building application ($configuration mode)..." -ForegroundColor Yellow
    dotnet build --configuration $configuration --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    
    # Set environment to Development (unless Release build requested)
    if (-not $Release) {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        Write-Host "?? Environment: DEVELOPMENT" -ForegroundColor Yellow
    } else {
        $env:ASPNETCORE_ENVIRONMENT = "Production"
        Write-Host "?? Environment: PRODUCTION" -ForegroundColor Red
    }
    
    # Find the built executable
    $exePath = "bin\$configuration\net8.0\EcommerceStarter.exe"
    
    if (-not (Test-Path $exePath)) {
        throw "Executable not found at: $exePath"
    }
    
    Write-Host "?? Launching application..." -ForegroundColor Cyan
    Write-Host "   Executable: $exePath" -ForegroundColor Gray
    Write-Host "   Environment: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? Application URLs:" -ForegroundColor Green
    Write-Host "   HTTP:  http://localhost:5000" -ForegroundColor Gray
    Write-Host "   HTTPS: https://localhost:5001" -ForegroundColor Gray
    Write-Host "   Admin: https://localhost:5001/Admin/Dashboard" -ForegroundColor Gray
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Launch the application
    $process = Start-Process -FilePath $exePath -PassThru -Wait
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "?? Application closed (Exit Code: $($process.ExitCode))" -ForegroundColor Cyan
}
catch {
    Write-Host "? Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
