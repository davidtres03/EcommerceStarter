# Get token from Windows Credential Manager
Add-Type -AssemblyName System.Security
$credentialName = "CatalystGitHubToken"

Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    public class CredentialManager
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Credential
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CredFree(IntPtr cred);

        public static string GetPassword(string target)
        {
            IntPtr credPtr;
            if (CredRead(target, 1, 0, out credPtr))
            {
                Credential cred = (Credential)Marshal.PtrToStructure(credPtr, typeof(Credential));
                string password = Marshal.PtrToStringUni(cred.CredentialBlob, cred.CredentialBlobSize / 2);
                CredFree(credPtr);
                return password;
            }
            return null;
        }
    }
"@

$token = [CredentialManager]::GetPassword($credentialName)

if ([string]::IsNullOrEmpty($token)) {
    Write-Host "? Could not retrieve token" -ForegroundColor Red
    exit 1
}

$headers = @{
    Authorization = "Bearer $token"
    Accept = "application/vnd.github+json"
}

# Step 1: Delete old release from CapAndCollarSupplyCo
Write-Host "Step 1: Cleaning up old release from CapAndCollarSupplyCo..." -ForegroundColor Cyan
try {
    $oldRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/CapAndCollarSupplyCo/releases/tags/v1.0.1" -Headers $headers
    Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/CapAndCollarSupplyCo/releases/$($oldRelease.id)" -Method Delete -Headers $headers
    Write-Host "? Old release deleted" -ForegroundColor Green
}
catch {
    Write-Host "?? Could not delete old release (may not exist)" -ForegroundColor Yellow
}

# Step 2: Create release in EcommerceStarter repository
Write-Host "`nStep 2: Creating release in EcommerceStarter repository..." -ForegroundColor Cyan

$releaseBody = @"
## What's New in v1.0.1

? **Demo Mode Complete**
- Shift key easter egg for testing
- Beautiful GUI demo launcher
- All operations simulated safely

? **UI Fixes**
- Fixed scrolling on Maintenance page
- Fixed scrolling on Configuration page
- Fixed all emoji rendering issues

? **Upgrade Support**
- GitHub versioning integration
- Automatic update detection
- Demo mode compatible upgrades

? **Improvements**
- DemoStateService for mock installations
- Better error handling
- Enhanced user experience

**God bless! ??**
"@

$body = @{
    tag_name = "v1.0.1"
    target_commitish = "clean-main"
    name = "v1.0.1 - Demo Mode & Fixes"
    body = $releaseBody
    draft = $false
    prerelease = $false
} | ConvertTo-Json

try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/EcommerceStarter/releases" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    
    Write-Host "? Release created in EcommerceStarter!" -ForegroundColor Green
    Write-Host "Release URL: $($release.html_url)" -ForegroundColor Green
    
    # Step 3: Upload installer
    Write-Host "`nStep 3: Uploading installer..." -ForegroundColor Cyan
    
    $installerPath = "C:\Dev\Websites\EcommerceStarter.Installer\bin\Release\net8.0-windows\EcommerceStarter.Installer.exe"
    
    if (-not (Test-Path $installerPath)) {
        Write-Host "? Installer not found at: $installerPath" -ForegroundColor Red
        exit 1
    }
    
    $fileInfo = Get-Item $installerPath
    Write-Host "Uploading: $($fileInfo.Name) ($([math]::Round($fileInfo.Length/1MB,2)) MB)" -ForegroundColor Cyan
    
    $uploadUrl = $release.upload_url -replace '\{\?name,label\}', "?name=EcommerceStarter-Installer-v1.0.1.exe"
    
    $uploadHeaders = @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/octet-stream"
    }
    
    $fileBytes = [System.IO.File]::ReadAllBytes($installerPath)
    $asset = Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $uploadHeaders -Body $fileBytes
    
    Write-Host "? Installer uploaded!" -ForegroundColor Green
    Write-Host "Download URL: $($asset.browser_download_url)" -ForegroundColor Green
    Write-Host "`n?? Release v1.0.1 ready in EcommerceStarter repository!" -ForegroundColor Green
    Write-Host "The upgrade should work now! ??" -ForegroundColor Green
}
catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
