# Package EcommerceStarter application and upload to GitHub release
param(
    [string]$Version = "1.0.1"
)

Write-Host "Packaging EcommerceStarter v$Version..." -ForegroundColor Cyan

# Build the application
Write-Host "`nStep 1: Building application..." -ForegroundColor Cyan
cd C:\Dev\Websites\EcommerceStarter
dotnet publish -c Release -o bin\Release\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create ZIP
Write-Host "`nStep 2: Creating ZIP package..." -ForegroundColor Cyan
$publishPath = "C:\Dev\Websites\EcommerceStarter\bin\Release\publish"
$zipPath = "C:\Dev\Websites\EcommerceStarter-v$Version.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "Created: $zipPath" -ForegroundColor Green
$zipSize = (Get-Item $zipPath).Length / 1MB
Write-Host "Size: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Green

# Upload to GitHub
Write-Host "`nStep 3: Uploading to GitHub release..." -ForegroundColor Cyan

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

# Get the release
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/EcommerceStarter/releases/tags/v$Version" -Headers $headers

Write-Host "Found release: $($release.name)" -ForegroundColor Green

# Upload the ZIP
$uploadUrl = $release.upload_url -replace '\{\?name,label\}', "?name=EcommerceStarter-v$Version.zip"
$uploadHeaders = @{ Authorization = "Bearer $token"; "Content-Type" = "application/zip" }

Write-Host "Uploading ZIP (this may take a minute)..." -ForegroundColor Yellow
$fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
$asset = Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $uploadHeaders -Body $fileBytes

Write-Host "? Application ZIP uploaded!" -ForegroundColor Green
Write-Host "Download URL: $($asset.browser_download_url)" -ForegroundColor Green

Write-Host "`n?? Release v$Version is complete!" -ForegroundColor Green
Write-Host "Assets:" -ForegroundColor Cyan
Write-Host "  - Installer: EcommerceStarter-Installer-v$Version.exe" -ForegroundColor White
Write-Host "  - Application: EcommerceStarter-v$Version.zip" -ForegroundColor White
Write-Host "`nThe upgrade should work now! ??" -ForegroundColor Green
