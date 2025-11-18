# DEPRECATED: This script was for private repo releases
# Public EcommerceStarter releases should use GitHub Actions workflow
# See: .github/workflows/release.yml
Write-Host "⚠️ This script is deprecated. Use GitHub Actions for releases." -ForegroundColor Yellow
exit 1

try {
    # Load credential from Credential Manager
    $cred = [System.Net.NetworkCredential]::new("", (cmdkey /list | Select-String -Pattern $credentialName | Out-String))

    # Better approach - use direct Windows API
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
}
catch {
    Write-Host "? Error accessing Credential Manager: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{
    Authorization = "Bearer $token"
    Accept        = "application/vnd.github+json"
}

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

**God bless! Victory**
"@

$body = @{
    tag_name         = "v1.0.1"
    target_commitish = "clean-main"
    name             = "v1.0.1 - Demo Mode & Fixes"
    body             = $releaseBody
    draft            = $false
    prerelease       = $false
} | ConvertTo-Json

Write-Host "Creating GitHub Release v1.0.1..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/davidtres03/CapAndCollarSupplyCo/releases" -Method Post -Headers $headers -Body $body -ContentType "application/json"

    Write-Host "? Release created successfully!" -ForegroundColor Green
    Write-Host "Release URL: $($response.html_url)" -ForegroundColor Green
    Write-Host "Upload URL: $($response.upload_url)" -ForegroundColor Yellow
}
catch {
    Write-Host "? Error creating release:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}
