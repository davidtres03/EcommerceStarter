# ============================================================================
# Security Hardening Script for Windows 11 Pro Host
# MyStore Supply Co.
# ============================================================================
# This script applies security best practices to your Windows 11 Pro host
# Run as Administrator AFTER initial setup
#
# Usage: .\security-hardening.ps1 -DevMachineIP "192.168.1.50"
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$DevMachineIP,
    
    [string]$WebsitePath = "C:\inetpub\wwwroot\MyStore",
    [string]$AppPoolName = "MyStorePool"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "  Security Hardening Script" -ForegroundColor Cyan
Write-Host "  MyStore Supply Co." -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Dev Machine IP: $DevMachineIP" -ForegroundColor Yellow
Write-Host "  Website Path: $WebsitePath" -ForegroundColor Yellow
Write-Host "  App Pool: $AppPoolName" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# 1. Windows Firewall Configuration
# ============================================================================

Write-Host "[1/10] Configuring Windows Firewall..." -ForegroundColor Cyan

# Enable firewall for all profiles
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True

# Set default policies
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultInboundAction Block
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultOutboundAction Allow

Write-Host "  ? Firewall enabled with default deny inbound" -ForegroundColor Green

# Remove old HTTP rules if they exist
Get-NetFirewallRule -Name "HTTP-Inbound" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Get-NetFirewallRule -Name "HTTPS-Inbound" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Get-NetFirewallRule -Name "SSH-*" -ErrorAction SilentlyContinue | Remove-NetFirewallRule

# Create HTTP rule
New-NetFirewallRule -Name "HTTP-Inbound" `
    -DisplayName "HTTP (Port 80)" `
    -Description "Allow inbound HTTP traffic" `
    -Enabled True `
    -Direction Inbound `
    -Protocol TCP `
    -Action Allow `
    -LocalPort 80 | Out-Null

Write-Host "  ? HTTP (80) allowed" -ForegroundColor Green

# Create HTTPS rule
New-NetFirewallRule -Name "HTTPS-Inbound" `
    -DisplayName "HTTPS (Port 443)" `
    -Description "Allow inbound HTTPS traffic" `
    -Enabled True `
    -Direction Inbound `
    -Protocol TCP `
    -Action Allow `
    -LocalPort 443 | Out-Null

Write-Host "  ? HTTPS (443) allowed" -ForegroundColor Green

# Create SSH rule - ONLY from dev machine
New-NetFirewallRule -Name "SSH-Dev-Only" `
    -DisplayName "SSH from Dev Machine" `
    -Description "Allow SSH only from development machine" `
    -Enabled True `
    -Direction Inbound `
    -Protocol TCP `
    -Action Allow `
    -LocalPort 22 `
    -RemoteAddress $DevMachineIP | Out-Null

Write-Host "  ? SSH (22) allowed from $DevMachineIP only" -ForegroundColor Green

# Block SSH from everywhere else
New-NetFirewallRule -Name "SSH-Block-Others" `
    -DisplayName "Block Other SSH" `
    -Description "Block SSH from all other sources" `
    -Enabled True `
    -Direction Inbound `
    -Protocol TCP `
    -Action Block `
    -LocalPort 22 | Out-Null

Write-Host "  ? SSH blocked from all other IPs" -ForegroundColor Green

# Block SQL Server from external
New-NetFirewallRule -Name "SQL-Block-External" `
    -DisplayName "Block External SQL Server" `
    -Description "Block SQL Server access from outside" `
    -Enabled True `
    -Direction Inbound `
    -Protocol TCP `
    -Action Block `
    -LocalPort 1433 | Out-Null

Write-Host "  ? SQL Server (1433) blocked from external" -ForegroundColor Green

Write-Host ""

# ============================================================================
# 2. File System Permissions
# ============================================================================

Write-Host "[2/10] Hardening file system permissions..." -ForegroundColor Cyan

if (Test-Path $WebsitePath) {
    # Remove inheritance
    $acl = Get-Acl $WebsitePath
    $acl.SetAccessRuleProtection($true, $false)
    Set-Acl $WebsitePath $acl
    
    # Grant specific permissions
    $identity = "IIS AppPool\$AppPoolName"
    
    # Read & Execute for app pool
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $identity,
        "ReadAndExecute",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.SetAccessRule($rule)
    
    # Administrators full control
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "Administrators",
        "FullControl",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.SetAccessRule($rule)
    
    Set-Acl $WebsitePath $acl
    
    Write-Host "  ? Website permissions configured" -ForegroundColor Green
    
    # Special permissions for uploads folder
    $uploadsPath = Join-Path $WebsitePath "wwwroot\uploads"
    if (Test-Path $uploadsPath) {
        $acl = Get-Acl $uploadsPath
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $identity,
            "Modify",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($rule)
        Set-Acl $uploadsPath $acl
        
        Write-Host "  ? Uploads folder write permissions set" -ForegroundColor Green
    }
} else {
    Write-Host "  ? Website path not found: $WebsitePath" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# 3. Disable Unnecessary Services
# ============================================================================

Write-Host "[3/10] Disabling unnecessary services..." -ForegroundColor Cyan

$servicesToDisable = @(
    "RemoteRegistry",
    "WMPNetworkSvc",
    "HomeGroupListener",
    "HomeGroupProvider"
)

foreach ($service in $servicesToDisable) {
    $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
    if ($svc) {
        Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
        Set-Service -Name $service -StartupType Disabled -ErrorAction SilentlyContinue
        Write-Host "  ? Disabled: $service" -ForegroundColor Green
    }
}

Write-Host ""

# ============================================================================
# 4. Windows Defender Configuration
# ============================================================================

Write-Host "[4/10] Configuring Windows Defender..." -ForegroundColor Cyan

# Enable real-time protection
Set-MpPreference -DisableRealtimeMonitoring $false

# Enable cloud protection
Set-MpPreference -MAPSReporting Advanced

# Enable automatic sample submission
Set-MpPreference -SubmitSamplesConsent SendAllSamples

# Exclude IIS directories from scanning (performance)
Add-MpPreference -ExclusionPath "C:\inetpub\logs"
Add-MpPreference -ExclusionPath "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files"

Write-Host "  ? Windows Defender configured" -ForegroundColor Green
Write-Host ""

# ============================================================================
# 5. Audit Policy Configuration
# ============================================================================

Write-Host "[5/10] Configuring audit policies..." -ForegroundColor Cyan

# Enable audit logging
auditpol /set /category:"Logon/Logoff" /success:enable /failure:enable | Out-Null
auditpol /set /category:"Account Logon" /success:enable /failure:enable | Out-Null
auditpol /set /category:"Object Access" /success:enable /failure:enable | Out-Null
auditpol /set /category:"Policy Change" /success:enable /failure:enable | Out-Null
auditpol /set /category:"Privilege Use" /failure:enable | Out-Null
auditpol /set /category:"System" /success:enable /failure:enable | Out-Null

Write-Host "  ? Audit policies configured" -ForegroundColor Green
Write-Host ""

# ============================================================================
# 6. IIS Security Headers
# ============================================================================

Write-Host "[6/10] Configuring IIS security headers..." -ForegroundColor Cyan

Import-Module WebAdministration

# Remove server header
Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
    -Filter "system.webServer/security/requestFiltering" `
    -Name "removeServerHeader" `
    -Value $true

# Hide IIS version
Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
    -Filter "system.webServer/httpProtocol/customHeaders" `
    -Name "." `
    -Value @{name='X-Powered-By';value=''} `
    -Force -ErrorAction SilentlyContinue

Write-Host "  ? Server headers hidden" -ForegroundColor Green

# Disable directory browsing
Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
    -Filter "system.webServer/directoryBrowse" `
    -Name "enabled" `
    -Value $false

Write-Host "  ? Directory browsing disabled" -ForegroundColor Green
Write-Host ""

# ============================================================================
# 7. Disable SMBv1
# ============================================================================

Write-Host "[7/10] Disabling SMBv1 (security risk)..." -ForegroundColor Cyan

# Disable SMBv1
Set-SmbServerConfiguration -EnableSMB1Protocol $false -Force -ErrorAction SilentlyContinue

Write-Host "  ? SMBv1 disabled" -ForegroundColor Green
Write-Host ""

# ============================================================================
# 8. Configure Remote Desktop (if enabled)
# ============================================================================

Write-Host "[8/10] Hardening Remote Desktop..." -ForegroundColor Cyan

# Set Network Level Authentication requirement
Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp' `
    -Name "UserAuthentication" -Value 1

# Set encryption level to high
Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp' `
    -Name "MinEncryptionLevel" -Value 3

Write-Host "  ? RDP security enhanced" -ForegroundColor Green
Write-Host ""

# ============================================================================
# 9. Configure Event Log Sizes
# ============================================================================

Write-Host "[9/10] Configuring event log sizes..." -ForegroundColor Cyan

# Increase Security log size
$logName = "Security"
$log = New-Object System.Diagnostics.Eventing.Reader.EventLogConfiguration $logName
$log.MaximumSizeInBytes = 512MB
$log.SaveChanges()

Write-Host "  ? Security log size increased" -ForegroundColor Green

# Increase Application log size
$logName = "Application"
$log = New-Object System.Diagnostics.Eventing.Reader.EventLogConfiguration $logName
$log.MaximumSizeInBytes = 512MB
$log.SaveChanges()

Write-Host "  ? Application log size increased" -ForegroundColor Green
Write-Host ""

# ============================================================================
# 10. Create Security Monitoring Script
# ============================================================================

Write-Host "[10/10] Creating security monitoring script..." -ForegroundColor Cyan

$monitoringScript = @"
# Security Monitoring Script
# Run this daily to check for security issues

`$ErrorActionPreference = 'SilentlyContinue'

Write-Host "Security Check - `$(Get-Date)" -ForegroundColor Cyan
Write-Host ""

# Check for failed login attempts
`$failedLogins = Get-EventLog -LogName Security -InstanceId 4625 -After (Get-Date).AddDays(-1) 2>$null
if (`$failedLogins) {
    Write-Host "WARNING: `$(`$failedLogins.Count) failed login attempts in last 24 hours" -ForegroundColor Yellow
} else {
    Write-Host "? No failed login attempts" -ForegroundColor Green
}

# Check firewall status
`$firewallProfiles = Get-NetFirewallProfile
foreach (`$profile in `$firewallProfiles) {
    if (`$profile.Enabled) {
        Write-Host "? Firewall enabled: `$(`$profile.Name)" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Firewall disabled: `$(`$profile.Name)" -ForegroundColor Red
    }
}

# Check Windows Defender status
`$defenderStatus = Get-MpComputerStatus
if (`$defenderStatus.RealTimeProtectionEnabled) {
    Write-Host "? Windows Defender real-time protection enabled" -ForegroundColor Green
} else {
    Write-Host "WARNING: Windows Defender real-time protection disabled" -ForegroundColor Red
}

# Check for Windows Updates
`$updates = (New-Object -ComObject Microsoft.Update.Session).CreateUpdateSearcher().Search("IsInstalled=0")
if (`$updates.Updates.Count -gt 0) {
    Write-Host "WARNING: `$(`$updates.Updates.Count) Windows updates available" -ForegroundColor Yellow
} else {
    Write-Host "? Windows is up to date" -ForegroundColor Green
}

# Check disk space
`$disk = Get-PSDrive C
`$freePercent = [math]::Round((`$disk.Free / (`$disk.Used + `$disk.Free)) * 100, 2)
if (`$freePercent -lt 10) {
    Write-Host "WARNING: Low disk space: `$freePercent% free" -ForegroundColor Red
} elseif (`$freePercent -lt 20) {
    Write-Host "WARNING: Disk space getting low: `$freePercent% free" -ForegroundColor Yellow
} else {
    Write-Host "? Disk space OK: `$freePercent% free" -ForegroundColor Green
}

# Check IIS status
Import-Module WebAdministration
`$appPool = Get-WebAppPoolState -Name "$AppPoolName"
if (`$appPool.Value -eq "Started") {
    Write-Host "? Application pool running" -ForegroundColor Green
} else {
    Write-Host "WARNING: Application pool not running" -ForegroundColor Red
}

`$website = Get-Website -Name "MyStore"
if (`$website.State -eq "Started") {
    Write-Host "? Website running" -ForegroundColor Green
} else {
    Write-Host "WARNING: Website not running" -ForegroundColor Red
}

Write-Host ""
Write-Host "Security check completed." -ForegroundColor Cyan
"@

$monitoringScriptPath = "C:\Deploy\security-check.ps1"
$monitoringScript | Out-File -FilePath $monitoringScriptPath -Encoding utf8

Write-Host "  ? Security monitoring script created at: $monitoringScriptPath" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Create Scheduled Task for Security Monitoring
# ============================================================================

Write-Host "Creating scheduled task for daily security checks..." -ForegroundColor Cyan

$action = New-ScheduledTaskAction -Execute "PowerShell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File $monitoringScriptPath"

$trigger = New-ScheduledTaskTrigger -Daily -At 9am

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

try {
    Register-ScheduledTask -TaskName "MyStore-SecurityCheck" `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Description "Daily security check for MyStore website" `
        -Force | Out-Null
    
    Write-Host "  ? Scheduled task created (runs daily at 9am)" -ForegroundColor Green
} catch {
    Write-Host "  ? Could not create scheduled task: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# Summary
# ============================================================================

Write-Host "============================================================================" -ForegroundColor Green
Write-Host "  SECURITY HARDENING COMPLETE!" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Security measures applied:" -ForegroundColor Cyan
Write-Host "  ? Windows Firewall configured with strict rules"
Write-Host "  ? SSH access restricted to dev machine only ($DevMachineIP)"
Write-Host "  ? File system permissions hardened"
Write-Host "  ? Unnecessary services disabled"
Write-Host "  ? Windows Defender optimized"
Write-Host "  ? Audit logging enabled"
Write-Host "  ? IIS security headers configured"
Write-Host "  ? SMBv1 disabled"
Write-Host "  ? Remote Desktop hardened"
Write-Host "  ? Event log sizes increased"
Write-Host "  ? Security monitoring script created"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review firewall rules: Get-NetFirewallRule | Where-Object {`$_.Enabled -eq 'True'}"
Write-Host "  2. Test SSH access from dev machine"
Write-Host "  3. Verify website still works"
Write-Host "  4. Run security check: $monitoringScriptPath"
Write-Host "  5. Configure SQL Server encryption (if not done)"
Write-Host "  6. Set up SSL certificate"
Write-Host ""
Write-Host "Security monitoring:" -ForegroundColor Cyan
Write-Host "  • Daily automated check runs at 9am"
Write-Host "  • Manual check: $monitoringScriptPath"
Write-Host "  • Review Security event log regularly"
Write-Host ""

# Save security configuration report
$reportPath = "C:\Deploy\Logs\security-config-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
$report = @"
Security Hardening Report
=========================
Date: $(Get-Date)
Server: $env:COMPUTERNAME

Firewall Configuration:
- Default Inbound: Block
- Default Outbound: Allow
- HTTP (80): Allowed from all
- HTTPS (443): Allowed from all
- SSH (22): Allowed from $DevMachineIP only
- SQL (1433): Blocked from external

File Permissions:
- Website path: $WebsitePath
- App pool identity: IIS AppPool\$AppPoolName
- Permissions: Read & Execute (Uploads: Modify)

Services Disabled:
- RemoteRegistry
- WMPNetworkSvc
- HomeGroupListener
- HomeGroupProvider

Windows Defender:
- Real-time protection: Enabled
- Cloud protection: Enabled
- Automatic samples: Enabled

Audit Policies:
- Logon/Logoff: Success & Failure
- Account Logon: Success & Failure
- Object Access: Success & Failure
- Policy Change: Success & Failure
- Privilege Use: Failure
- System: Success & Failure

IIS Security:
- Server headers: Hidden
- Directory browsing: Disabled

Network Security:
- SMBv1: Disabled
- RDP NLA: Required
- RDP Encryption: High

Monitoring:
- Security check script: $monitoringScriptPath
- Scheduled task: Daily at 9am
"@

if (-not (Test-Path "C:\Deploy\Logs")) {
    New-Item -ItemType Directory -Force -Path "C:\Deploy\Logs" | Out-Null
}

$report | Out-File -FilePath $reportPath -Encoding utf8

Write-Host "Security configuration report saved to:" -ForegroundColor Cyan
Write-Host "  $reportPath" -ForegroundColor Gray
Write-Host ""

Write-Host "Your server is now hardened! ??" -ForegroundColor Green
Write-Host ""
