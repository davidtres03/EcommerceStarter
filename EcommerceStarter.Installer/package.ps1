param(
  [string]$Configuration = "Release",
  [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Checked {
  param(
    [Parameter(Mandatory = $true)][string]$FilePath,
    [Parameter(Mandatory = $true)][string[]]$Arguments
  )
  Write-Host "→ $FilePath $($Arguments -join ' ')"
  & $FilePath @Arguments | Out-Host
  if ($LASTEXITCODE -ne 0) {
    throw "Command failed with exit code ${LASTEXITCODE}: ${FilePath} $($Arguments -join ' ')"
  }
}

# Paths
$solutionRoot = Split-Path -Parent $PSScriptRoot  # ...\FungalSupplyCo
$installerProj = Join-Path $PSScriptRoot "EcommerceStarter.Installer.csproj"

# Web app is in the same workspace
$webRoot = $solutionRoot
$webProj = Join-Path $webRoot "EcommerceStarter\EcommerceStarter.csproj"
$migrationsExe = Join-Path $webRoot "migrations\efbundle.exe"

# Windows Service is sibling project under this solution root
$serviceProj = Join-Path $solutionRoot "EcommerceStarter.WindowsService\EcommerceStarter.WindowsService.csproj"
# Upgrader is sibling project
$upgraderProj = Join-Path $solutionRoot "EcommerceStarter.Upgrader\EcommerceStarter.Upgrader.csproj"

# Publish output folders
$publishRoot = Join-Path $PSScriptRoot "bin\$Configuration\net8.0-windows\$Runtime\publish"
$appOut = Join-Path $publishRoot "app"
$migOut = Join-Path $publishRoot "migrations"
$svcOut = Join-Path $publishRoot "WindowsService"
$upgraderOut = Join-Path $publishRoot "Upgrader"

Write-Host "Publishing installer ($Configuration, $Runtime)..."

# Ensure a clean publish root for the installer EXE to land where we expect
if (Test-Path $publishRoot) {
  Write-Host "Cleaning previous publish at '$publishRoot'..."

  # Try to stop any running installer processes that may lock the EXE
  try {
    $procs = Get-Process -Name "EcommerceStarter.Installer" -ErrorAction SilentlyContinue
    if ($procs) {
      Write-Host "Stopping running installer processes..."
      $procs | Stop-Process -Force -ErrorAction SilentlyContinue
      Start-Sleep -Seconds 1
    }
  }
  catch {}

  try {
    Remove-Item -Recurse -Force $publishRoot -ErrorAction Stop
  }
  catch {
    Write-Warning "Could not fully clean publish folder (files may be locked). Attempting partial cleanup..."
    # Try to remove all items inside except the EXE if locked
    Get-ChildItem -Force $publishRoot | ForEach-Object {
      try { Remove-Item -Recurse -Force $_.FullName -ErrorAction Stop } catch { }
    }
  }
}

# Force publish output to the expected $publishRoot regardless of TargetFramework (e.g., net8/net9)
Invoke-Checked "dotnet" @(
  "publish", $installerProj, "-c", $Configuration, "-r", $Runtime,
  "-p:PublishSingleFile=true", "--self-contained", "true", "-o", $publishRoot
)

# Ensure publish root exists (in case publish did not create it for some reason)
New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null

Write-Host "Publishing web app to '$appOut'..."
Invoke-Checked "dotnet" @(
  "publish", $webProj, "-c", $Configuration, "-o", $appOut
)

# Clean up Application folder if it exists
$appDir = Join-Path $publishRoot "app"
$applicationDir = Join-Path $publishRoot "Application"
if (Test-Path $applicationDir) {
  Write-Host "Removing existing Application folder..."
  Remove-Item -Recurse -Force $applicationDir
}
# Rename app folder to Application (installer expects this name)
if (Test-Path $appDir) {
  Write-Host "Renaming app folder to Application..."
  Rename-Item -Path $appDir -NewName "Application" -Force
}

if (Test-Path "$migrationsExe") {
  Write-Host "Copying migrations to '$migOut'..."
  New-Item -ItemType Directory -Force -Path $migOut | Out-Null
  Copy-Item -Force "$migrationsExe" "$migOut\efbundle.exe"
}
else {
  Write-Warning "migrations/efbundle.exe not found at $migrationsExe"
}

if (Test-Path "$serviceProj") {
  Write-Host "Publishing Windows Service to '$svcOut'..."
  Invoke-Checked "dotnet" @(
    "publish", $serviceProj, "-c", $Configuration, "-o", $svcOut
  )
}
else {
  Write-Warning "Windows Service project not found: $serviceProj"
}

if (Test-Path "$upgraderProj") {
  Write-Host "Publishing Upgrader to '$upgraderOut'..."
  Invoke-Checked "dotnet" @(
    "publish", $upgraderProj, "-c", $Configuration, "-o", $upgraderOut
  )
}
else {
  Write-Warning "Upgrader project not found: $upgraderProj"
}

# Verify critical artifacts
$installerExe = Join-Path $publishRoot "EcommerceStarter.Installer.exe"
if (-not (Test-Path $installerExe)) {
  throw "Missing installer EXE: $installerExe"
}

$applicationDir = Join-Path $publishRoot "Application"
if (-not (Test-Path $applicationDir)) {
  throw "Missing Application folder after publish/rename: $applicationDir"
}

$webDll = Join-Path $applicationDir "EcommerceStarter.dll"
if (-not (Test-Path $webDll)) {
  throw "Missing EcommerceStarter.dll in Application folder: $webDll"
}

if (-not (Test-Path $svcOut)) {
  throw "Missing WindowsService folder: $svcOut"
}

$migrationsPath = Join-Path $publishRoot "migrations\efbundle.exe"
if (-not (Test-Path $migrationsPath)) {
  Write-Warning "migrations/efbundle.exe not found at $migrationsPath"
}

Write-Host "\nPackage ready at: $publishRoot"
Write-Host "Contains:"
Get-ChildItem $publishRoot | Select-Object Name, Length | Format-Table | Out-Host

Write-Host "\nSummary:"
Write-Host "- Installer: $installerExe"
Write-Host "- Application DLL: $webDll"
Write-Host "- Windows Service: $svcOut"
Write-Host "- Migrations: $migrationsPath (" -NoNewline; if (Test-Path $migrationsPath) { Write-Host "present)" } else { Write-Host "missing)" }

# Create release ZIP for distribution
Write-Host "`nCreating release package..."
$version = "1.0.5" # TODO: Read from .csproj
$releaseZip = Join-Path (Split-Path $publishRoot) "EcommerceStarter-v$version.zip"

# Remove existing ZIP if present
if (Test-Path $releaseZip) {
  Write-Host "Removing existing release ZIP..."
  Remove-Item $releaseZip -Force
}

# Create ZIP
Write-Host "Compressing publish folder to: $releaseZip"
Compress-Archive -Path "$publishRoot\*" -DestinationPath $releaseZip -CompressionLevel Optimal

$zipSize = (Get-Item $releaseZip).Length
$zipSizeMB = [math]::Round($zipSize / 1MB, 2)
Write-Host "✓ Release package created: $releaseZip ($zipSizeMB MB)" -ForegroundColor Green

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "RELEASE READY" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Distribution file: $releaseZip" -ForegroundColor Yellow
Write-Host "Size: $zipSizeMB MB" -ForegroundColor Yellow
Write-Host "`nTo test: Extract ZIP and run EcommerceStarter.Installer.exe" -ForegroundColor White
Write-Host "==================================================" -ForegroundColor Cyan
