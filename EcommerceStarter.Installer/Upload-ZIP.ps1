# Upload ZIP to existing release
Add-Type @"
using System;using System.Runtime.InteropServices;
public class CM{[StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
public struct C{public int F;public int T;public string TN;public string Co;
public System.Runtime.InteropServices.ComTypes.FILETIME L;public int CS;public IntPtr CB;
public int P;public int AC;public IntPtr A;public string TA;public string U;}
[DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
public static extern bool CredRead(string t,int ty,int r,out IntPtr c);
[DllImport("advapi32.dll",SetLastError=true)]public static extern bool CredFree(IntPtr c);
public static string GP(string t){IntPtr p;if(CredRead(t,1,0,out p)){
C cr=(C)Marshal.PtrToStructure(p,typeof(C));string pw=Marshal.PtrToStringUni(cr.CB,cr.CS/2);
CredFree(p);return pw;}return null;}}
"@
$t=[CM]::GP('CatalystGitHubToken')
$h=@{Authorization="Bearer $t";Accept='application/vnd.github+json'}
$r=Invoke-RestMethod -Uri 'https://api.github.com/repos/davidtres03/EcommerceStarter/releases/tags/v1.0.1' -Headers $h
$u=$r.upload_url -replace '\{\?name,label\}','?name=EcommerceStarter-v1.0.1.zip'
$uh=@{Authorization="Bearer $t";'Content-Type'='application/zip'}
$fb=[System.IO.File]::ReadAllBytes('C:\Dev\Websites\EcommerceStarter-v1.0.1.zip')
Write-Host 'Uploading ZIP to GitHub (24 MB)...' -F Cyan
$a=Invoke-RestMethod -Uri $u -Method Post -Headers $uh -Body $fb
Write-Host "Done: $($a.browser_download_url)" -F Green
