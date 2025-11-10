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
    Write-Host "? Could not retrieve token from Credential Manager" -ForegroundColor Red
    exit 1
}

Write-Host "? Token retrieved from Credential Manager" -ForegroundColor Green

# Find the installer exe
$installerPath = Get-ChildItem -Recurse -Filter "*.exe" | Where-Object { $_.DirectoryName -like "*Release*publish*" } | Select-Object -First 1 -ExpandProperty FullName

if (-not $installerPath -or -not (Test-Path $installerPath)) {
    Write-Host "? Could not find published installer exe" -ForegroundColor Red
    Write-Host "Please build in Release mode first: dotnet publish -c Release" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found installer: $installerPath" -ForegroundColor Cyan
$fileInfo = Get-Item $installerPath
Write-Host "Size: $([math]::Round($fileInfo.Length/1MB,2)) MB" -ForegroundColor Cyan

# Get the release
$headers = @{
    Authorization = "Bearer $token"
    Accept = "application/vnd.github+json"
}

Write-Host "`nGetting release info..." -ForegroundColor Cyan

try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/CapAndCollarSupplyCo/releases/tags/v1.0.1" -Headers $headers
    
    Write-Host "Release ID: $($release.id)" -ForegroundColor Green
    
    # Upload the asset
    $uploadUrl = $release.upload_url -replace '\{\?name,label\}', "?name=EcommerceStarter-Installer-v1.0.1.exe"
    
    Write-Host "`nUploading installer to GitHub Release..." -ForegroundColor Cyan
    
    $uploadHeaders = @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/octet-stream"
    }
    
    $fileBytes = [System.IO.File]::ReadAllBytes($installerPath)
    
    $asset = Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $uploadHeaders -Body $fileBytes
    
    Write-Host "? Installer uploaded successfully!" -ForegroundColor Green
    Write-Host "Asset URL: $($asset.browser_download_url)" -ForegroundColor Green
    Write-Host "`n?? Release v1.0.1 is now complete with installer!" -ForegroundColor Green
}
catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
