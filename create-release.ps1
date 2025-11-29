# Create GitHub Release v1.0.4
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
    Write-Error 'Failed to retrieve GitHub token from Credential Manager'
    exit 1
}

$env:GH_TOKEN = $token
$zipPath = "EcommerceStarter.Installer/bin/Release/net8.0-windows/win-x64/EcommerceStarter-v1.0.4.zip"

Write-Host "Creating release v1.0.4..." -ForegroundColor Cyan
gh release create v1.0.4 $zipPath --title "v1.0.4 - Complete Package with Upgrader" --notes "Complete package structure with dedicated Upgrader.exe for in-place upgrades. Fixes auto-update flow. First release with all components properly packaged."
