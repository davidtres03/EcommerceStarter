# Create GitHub Release v1.0.4 via REST API
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public class CredentialManager {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Credential {
        public int Flags;
        public int Type;
        public string TargetName;
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
    public static extern bool CredFree(IntPtr buffer);
    public static string GetPassword(string target) {
        IntPtr credPtr;
        if (CredRead(target, 1, 0, out credPtr)) {
            var cred = Marshal.PtrToStructure<Credential>(credPtr);
            var password = Marshal.PtrToStringUni(cred.CredentialBlob, cred.CredentialBlobSize / 2);
            CredFree(credPtr);
            return password;
        }
        return null;
    }
}
'@

$token = [CredentialManager]::GetPassword('CatalystGitHubToken')
if (-not $token) {
    Write-Error 'Failed to retrieve GitHub token'
    exit 1
}

$owner = "davidtres03"
$repo = "EcommerceStarter"
$tag = "v1.0.4"
$zipPath = "EcommerceStarter.Installer/bin/Release/net8.0-windows/win-x64/EcommerceStarter-v1.0.4.zip"

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

Write-Host "Creating release v1.0.4..." -ForegroundColor Cyan

# Create release
$releaseBody = @{
    tag_name = $tag
    name = "v1.0.4 - Complete Package with Upgrader"
    body = "Complete package structure with dedicated Upgrader.exe for in-place upgrades. Fixes auto-update flow. First release with all components properly packaged."
    draft = $false
    prerelease = $false
} | ConvertTo-Json

try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases" -Method Post -Headers $headers -Body $releaseBody -ContentType "application/json"
    Write-Host "✓ Release created: $($release.html_url)" -ForegroundColor Green

    # Upload asset
    Write-Host "Uploading ZIP asset..." -ForegroundColor Cyan
    $uploadUrl = $release.upload_url -replace '\{.*\}', ''
    $fileName = Split-Path $zipPath -Leaf
    $zipBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $zipPath))

    $uploadHeaders = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/octet-stream"
    }

    $asset = Invoke-RestMethod -Uri "${uploadUrl}?name=$fileName" -Method Post -Headers $uploadHeaders -Body $zipBytes
    Write-Host "✓ Asset uploaded: $($asset.browser_download_url)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Release v1.0.4 complete! 🚀" -ForegroundColor Green

} catch {
    Write-Error "Failed: $($_.Exception.Message)"
    exit 1
}
