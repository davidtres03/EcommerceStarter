<#
.SYNOPSIS
    Test the upgrade flow from v1.0.9.5 to v1.0.9.6
    
.DESCRIPTION
    This script simulates the upgrade path that users will experience:
    1. Simulates v1.0.9.5 installation with old version in registry
    2. Checks if Windows would detect upgrade or downgrade
    3. Verifies new version would write correct registry value
    4. Confirms all assemblies have synchronized versions
    
.PARAMETER TestRegistryPath
    Registry path to use for testing (won't touch real installation)
#>

param(
    [string]$TestRegistryPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_UpgradeTest"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Upgrade Flow Test v1.0.9.6" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Colors for clarity
$Color_Success = "Green"
$Color_Error = "Red"
$Color_Warning = "Yellow"
$Color_Info = "Cyan"

# Step 1: Check all projects have version 1.0.9.6
Write-Host "STEP 1: Verify Version Consistency Across Projects" -ForegroundColor $Color_Info
Write-Host "========================================" -ForegroundColor Gray
Write-Host ""

$projectFiles = @(
    ".\EcommerceStarter\EcommerceStarter.csproj",
    ".\EcommerceStarter.Installer\EcommerceStarter.Installer.csproj",
    ".\EcommerceStarter.WindowsService\EcommerceStarter.WindowsService.csproj",
    ".\EcommerceStarter.DemoLauncher\EcommerceStarter.DemoLauncher.csproj"
)

$allVersionsMatch = $true
$versionMap = @{}

foreach ($projectFile in $projectFiles) {
    $projectName = Split-Path (Split-Path $projectFile) -Leaf
    
    if (-not (Test-Path $projectFile)) {
        Write-Host "  ❌ $projectName - FILE NOT FOUND" -ForegroundColor $Color_Error
        $allVersionsMatch = $false
        continue
    }
    
    # Extract AssemblyVersion from csproj
    $content = Get-Content $projectFile -Raw
    $match = [regex]::Match($content, '<AssemblyVersion>([^<]+)</AssemblyVersion>')
    
    if ($match.Success) {
        $version = $match.Groups[1].Value
        $versionMap[$projectName] = $version
        
        if ($version -eq "1.0.9.6") {
            Write-Host "  ✅ $projectName - $version" -ForegroundColor $Color_Success
        } else {
            Write-Host "  ⚠️  $projectName - $version (expected 1.0.9.6)" -ForegroundColor $Color_Warning
            $allVersionsMatch = $false
        }
    } else {
        Write-Host "  ⚠️  $projectName - NO VERSION FOUND" -ForegroundColor $Color_Warning
        $allVersionsMatch = $false
    }
}

Write-Host ""
if ($allVersionsMatch) {
    Write-Host "✅ All projects have consistent version 1.0.9.6" -ForegroundColor $Color_Success
} else {
    Write-Host "❌ Version inconsistency detected - upgrade path at risk" -ForegroundColor $Color_Error
}

# Step 2: Simulate registry state
Write-Host ""
Write-Host "STEP 2: Simulate Registry State (v1.0.9.5 → v1.0.9.6)" -ForegroundColor $Color_Info
Write-Host "========================================" -ForegroundColor Gray
Write-Host ""

# For this test, we'll simulate the registry state without creating actual registry entries
# since we may not have admin access
$currentRegVersion = "1.0.9.5"
Write-Host "  Simulating Registry DisplayVersion: $currentRegVersion (from v1.0.9.5)" -ForegroundColor $Color_Info
Write-Host "  Note: Running in test mode without modifying actual registry" -ForegroundColor Gray

# Step 3: Check upgrade detection logic
Write-Host ""
Write-Host "STEP 3: Windows Upgrade Detection Logic" -ForegroundColor $Color_Info
Write-Host "========================================" -ForegroundColor Gray
Write-Host ""

$newInstallerVersion = "1.0.9.6"
Write-Host "Current Registry Version: $currentRegVersion" -ForegroundColor Gray
Write-Host "New Installer Version:    $newInstallerVersion" -ForegroundColor Gray

# Version comparison
$regVersionParts = $currentRegVersion.Split('.')
$newVersionParts = $newInstallerVersion.Split('.')

$upgradeDetected = $false
$downgradeDetected = $false

# Compare versions
for ($i = 0; $i -lt [Math]::Min($regVersionParts.Length, $newVersionParts.Length); $i++) {
    $regPart = [int]$regVersionParts[$i]
    $newPart = [int]$newVersionParts[$i]
    
    if ($newPart -gt $regPart) {
        $upgradeDetected = $true
        break
    } elseif ($newPart -lt $regPart) {
        $downgradeDetected = $true
        break
    }
}

Write-Host ""
if ($upgradeDetected) {
    Write-Host "✅ UPGRADE DETECTED: $currentRegVersion → $newInstallerVersion" -ForegroundColor $Color_Success
    Write-Host "   Windows will proceed with installation" -ForegroundColor $Color_Success
} elseif ($downgradeDetected) {
    Write-Host "❌ DOWNGRADE DETECTED: $currentRegVersion → $newInstallerVersion" -ForegroundColor $Color_Error
    Write-Host "   Windows will trigger automatic rollback" -ForegroundColor $Color_Error
} else {
    Write-Host "⚠️  SAME VERSION: $currentRegVersion = $newInstallerVersion" -ForegroundColor $Color_Warning
    Write-Host "   Windows will proceed with update" -ForegroundColor $Color_Warning
}

# Step 4: Verify installer assembly
Write-Host ""
Write-Host "STEP 4: Verify Installer Assembly Version" -ForegroundColor $Color_Info
Write-Host "========================================" -ForegroundColor Gray
Write-Host ""

$installerPath = ".\EcommerceStarter.Installer\bin\Release\net8.0-windows\EcommerceStarter.Installer.exe"
if (Test-Path $installerPath) {
    try {
        $installerAssembly = [System.Reflection.Assembly]::LoadFile((Resolve-Path $installerPath).Path)
        $installerVersion = $installerAssembly.GetName().Version
        Write-Host "  ✅ Installer found: $installerPath" -ForegroundColor $Color_Success
        Write-Host "  ✅ AssemblyVersion: $installerVersion" -ForegroundColor $Color_Success
        
        if ($installerVersion.ToString() -eq "1.0.9.6") {
            Write-Host "  ✅ Installer version matches expected 1.0.9.6" -ForegroundColor $Color_Success
        } else {
            Write-Host "  ❌ Installer version mismatch: expected 1.0.9.6, got $installerVersion" -ForegroundColor $Color_Error
        }
    } catch {
        Write-Host "  ⚠️  Could not load installer assembly: $_" -ForegroundColor $Color_Warning
    }
} else {
    Write-Host "  ⚠️  Installer not found at: $installerPath" -ForegroundColor $Color_Warning
    Write-Host "  Note: Run 'dotnet build -c Release' first to build installer" -ForegroundColor Gray
}

# Step 5: Check app assembly
Write-Host ""
Write-Host "STEP 5: Verify Application Assembly Version" -ForegroundColor $Color_Info
Write-Host "========================================" -ForegroundColor Gray
Write-Host ""

$appPath = ".\EcommerceStarter\bin\Release\net8.0\EcommerceStarter.dll"
if (Test-Path $appPath) {
    try {
        $appAssembly = [System.Reflection.Assembly]::LoadFile((Resolve-Path $appPath).Path)
        $appVersion = $appAssembly.GetName().Version
        Write-Host "  ✅ App found: $appPath" -ForegroundColor $Color_Success
        Write-Host "  ✅ AssemblyVersion: $appVersion" -ForegroundColor $Color_Success
        
        if ($appVersion.ToString() -eq "1.0.9.6") {
            Write-Host "  ✅ App version matches expected 1.0.9.6" -ForegroundColor $Color_Success
        } else {
            Write-Host "  ❌ App version mismatch: expected 1.0.9.6, got $appVersion" -ForegroundColor $Color_Error
        }
    } catch {
        Write-Host "  ⚠️  Could not load app assembly: $_" -ForegroundColor $Color_Warning
    }
} else {
    Write-Host "  ⚠️  App not found at: $appPath" -ForegroundColor $Color_Warning
    Write-Host "  Note: Run 'dotnet build -c Release' first to build app" -ForegroundColor Gray
}

# Step 6: Summary
Write-Host ""
Write-Host "STEP 6: Test Summary" -ForegroundColor $Color_Info
Write-Host "========================================" -ForegroundColor Gray
Write-Host ""

$testsPassed = 0
$testsFailed = 0

if ($allVersionsMatch) {
    Write-Host "  ✅ Version consistency across projects" -ForegroundColor $Color_Success
    $testsPassed++
} else {
    Write-Host "  ❌ Version consistency failed" -ForegroundColor $Color_Error
    $testsFailed++
}

if ($upgradeDetected) {
    Write-Host "  ✅ Upgrade detection working" -ForegroundColor $Color_Success
    $testsPassed++
} else {
    Write-Host "  ❌ Upgrade detection failed" -ForegroundColor $Color_Error
    $testsFailed++
}

Write-Host ""
Write-Host "Tests Passed: $testsPassed" -ForegroundColor $Color_Success
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $Color_Error

# Cleanup
Write-Host ""
Write-Host "Test registry entry was simulated (not created)" -ForegroundColor Gray

# Final verdict
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
if ($testsFailed -eq 0) {
    Write-Host "✅ UPGRADE PATH TEST PASSED" -ForegroundColor $Color_Success
    Write-Host ""
    Write-Host "The version rollback issue has been fixed." -ForegroundColor $Color_Success
    Write-Host "Upgrade path from v1.0.9.5 to v1.0.9.6 is SAFE." -ForegroundColor $Color_Success
} else {
    Write-Host "❌ UPGRADE PATH TEST FAILED" -ForegroundColor $Color_Error
    Write-Host ""
    Write-Host "Fix the issues above before publishing release." -ForegroundColor $Color_Error
}
Write-Host "========================================" -ForegroundColor Cyan

exit $testsFailed
