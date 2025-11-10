# Quick script to create release and upload installer
Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    public class CredentialManager {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Credential {
            public int Flags; public int Type; public string TargetName; public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize; public IntPtr CredentialBlob; public int Persist;
            public int AttributeCount; public IntPtr Attributes; public string TargetAlias; public string UserName;
        }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CredFree(IntPtr cred);
        public static string GetPassword(string target) {
            IntPtr credPtr;
            if (CredRead(target, 1, 0, out credPtr)) {
                Credential cred = (Credential)Marshal.PtrToStructure(credPtr, typeof(Credential));
                string password = Marshal.PtrToStringUni(cred.CredentialBlob, cred.CredentialBlobSize / 2);
                CredFree(credPtr); return password;
            }
            return null;
        }
    }
"@

$token = [CredentialManager]::GetPassword("CatalystGitHubToken")
$headers = @{ Authorization = "Bearer $token"; Accept = "application/vnd.github+json" }

# Step 1: Create release
Write-Host "Creating v1.0.1 release..." -ForegroundColor Cyan
$releaseData = @{
    tag_name = "v1.0.1"
    name = "v1.0.1 - Demo Mode & Fixes"
    body = "## EcommerceStarter v1.0.1`n`n? Demo Mode, UI Fixes, Upgrade Support`n`n**God bless! ??**"
    draft = $false
    prerelease = $false
} | ConvertTo-Json

$release = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/EcommerceStarter/releases" -Method Post -Headers $headers -Body $releaseData -ContentType "application/json"
Write-Host "? Release created: $($release.html_url)" -ForegroundColor Green

# Step 2: Upload installer
Write-Host "`nUploading installer..." -ForegroundColor Cyan
$installerPath = "C:\Dev\Websites\EcommerceStarter.Installer\bin\Release\net8.0-windows\EcommerceStarter.Installer.exe"
$uploadUrl = $release.upload_url -replace '\{\?name,label\}', "?name=EcommerceStarter-Installer-v1.0.1.exe"
$uploadHeaders = @{ Authorization = "Bearer $token"; "Content-Type" = "application/octet-stream" }
$fileBytes = [System.IO.File]::ReadAllBytes($installerPath)
$asset = Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $uploadHeaders -Body $fileBytes

Write-Host "? Installer uploaded: $($asset.browser_download_url)" -ForegroundColor Green
Write-Host "`n?? DONE! Try the upgrade now!" -ForegroundColor Green
