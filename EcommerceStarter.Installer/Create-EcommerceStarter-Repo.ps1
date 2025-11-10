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

# Step 1: Create the new EcommerceStarter repository
Write-Host "Step 1: Creating new EcommerceStarter repository..." -ForegroundColor Cyan

$repoBody = @{
    name = "EcommerceStarter"
    description = "Professional E-Commerce Platform - Open Source Installer & Application"
    homepage = ""
    private = $false
    has_issues = $true
    has_projects = $true
    has_wiki = $true
    auto_init = $false
} | ConvertTo-Json

try {
    $repo = Invoke-RestMethod -Uri "https://api.github.com/user/repos" -Method Post -Headers $headers -Body $repoBody -ContentType "application/json"
    
    Write-Host "? Repository created!" -ForegroundColor Green
    Write-Host "URL: $($repo.html_url)" -ForegroundColor Green
    Write-Host "Clone URL: $($repo.clone_url)" -ForegroundColor Green
    
    # Step 2: Add new remote to local repo
    Write-Host "`nStep 2: Adding new remote to local git repository..." -ForegroundColor Cyan
    
    cd C:\Dev\Websites\EcommerceStarter.Installer
    
    # Check if remote already exists
    $existingRemote = git remote | Where-Object { $_ -eq "ecommerce" }
    if ($existingRemote) {
        git remote remove ecommerce
    }
    
    git remote add ecommerce "https://github.com/davidtres03/EcommerceStarter.git"
    Write-Host "? Remote 'ecommerce' added" -ForegroundColor Green
    
    # Step 3: Push to new repository
    Write-Host "`nStep 3: Pushing code to new repository..." -ForegroundColor Cyan
    git push ecommerce clean-main:main
    
    Write-Host "? Code pushed to EcommerceStarter repository!" -ForegroundColor Green
    
    # Step 4: Create the release
    Write-Host "`nStep 4: Creating v1.0.1 release..." -ForegroundColor Cyan
    
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

    $releaseData = @{
        tag_name = "v1.0.1"
        target_commitish = "main"
        name = "v1.0.1 - Demo Mode & Fixes"
        body = $releaseBody
        draft = $false
        prerelease = $false
    } | ConvertTo-Json
    
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/EcommerceStarter/releases" -Method Post -Headers $headers -Body $releaseData -ContentType "application/json"
    
    Write-Host "? Release created!" -ForegroundColor Green
    Write-Host "Release URL: $($release.html_url)" -ForegroundColor Green
    
    # Step 5: Upload installer
    Write-Host "`nStep 5: Uploading installer..." -ForegroundColor Cyan
    
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
    
    Write-Host "`n?? SUCCESS! EcommerceStarter repository is ready!" -ForegroundColor Green
    Write-Host "Repository: https://github.com/davidtres03/EcommerceStarter" -ForegroundColor Green
    Write-Host "Release: $($release.html_url)" -ForegroundColor Green
    Write-Host "`nThe upgrade should work now! ????" -ForegroundColor Green
}
catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
