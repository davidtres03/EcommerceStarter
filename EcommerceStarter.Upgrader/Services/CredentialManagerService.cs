using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for retrieving credentials from Windows Credential Manager
/// Used to automatically authenticate with private GitHub repositories during testing
/// </summary>
public class CredentialManagerService
{
    // Windows Credential Manager P/Invoke
    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(
        string target,
        CRED_TYPE type,
        int reservedFlag,
        out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr credentialPtr);

    private enum CRED_TYPE
    {
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4,
        GenericCertificate = 5,
        DomainExtended = 6,
        Maximum = 7,
        MaximumEx = Maximum + 1000
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    /// <summary>
    /// Retrieve GitHub Personal Access Token from Windows Credential Manager
    /// Looks for credential with target name "github.com/git"
    /// </summary>
    public static string? GetGitHubToken()
    {
        try
        {
            // Try common GitHub credential targets (in order of likelihood)
            var targets = new[]
            {
                "git:https://github.com",          // Git credential helper format
                "github.com/git",                  // Legacy format
                "git:https://davidtres03@github.com", // With username
                "gh:github.com:",                  // GitHub CLI format
                "gh_token",                        // Generic token name
                "github",                          // Simple name
                "CatalystGitHubToken"              // Custom name
            };

            foreach (var target in targets)
            {
                var token = ReadCredential(target);
                if (!string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine($"[CredentialManager] Found GitHub token in Credential Manager: {target}");
                    return token;
                }
            }

            System.Diagnostics.Debug.WriteLine("[CredentialManager] No GitHub token found in Credential Manager");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CredentialManager] Error retrieving GitHub token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Read a credential from Windows Credential Manager by target name
    /// </summary>
    private static string? ReadCredential(string targetName)
    {
        try
        {
            if (!CredRead(targetName, CRED_TYPE.Generic, 0, out var credentialPtr))
            {
                return null; // Credential not found - this is normal, not an error
            }

            try
            {
                // Marshal the credential structure
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

                // Extract the credential blob as a string
                if (credential.CredentialBlob != IntPtr.Zero && credential.CredentialBlobSize > 0)
                {
                    byte[] credentialBlob = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, credentialBlob, 0, (int)credential.CredentialBlobSize);
                    return Encoding.Unicode.GetString(credentialBlob);
                }

                return null;
            }
            finally
            {
                // Always free the credential pointer
                CredFree(credentialPtr);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CredentialManager] Error reading credential '{targetName}': {ex.Message}");
            return null;
        }
    }
}
