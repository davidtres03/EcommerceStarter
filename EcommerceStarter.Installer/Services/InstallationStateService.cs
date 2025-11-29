using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for detecting and managing installation state
/// </summary>
public class InstallationStateService
{
    private const string BaseRegistryPath = @"SOFTWARE\EcommerceStarter";
    private const string VersionKey = "Version"; // Preferred key written by installer
    private const string InstalledVersionKey = "InstalledVersion"; // Legacy fallback
    private const string InstallPathKey = "InstallPath";
    private const string InstallDateKey = "InstallDate";

    /// <summary>
    /// Check if any EcommerceStarter instance is already installed
    /// </summary>
    public bool IsInstalled()
    {
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(BaseRegistryPath);
            if (baseKey == null) return false;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                using var siteKey = baseKey.OpenSubKey(subKeyName);
                if (siteKey == null) continue;
                var version = siteKey.GetValue(VersionKey)?.ToString() ?? siteKey.GetValue(InstalledVersionKey)?.ToString();
                if (!string.IsNullOrEmpty(version)) return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get installation information for the first detected instance (for backward compatibility)
    /// </summary>
    public InstallationInfo? GetInstallationInfo()
    {
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(BaseRegistryPath);
            if (baseKey == null) return null;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                var info = GetInstallationInfo(subKeyName);
                if (info != null) return info;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get installation information for a specific site instance
    /// </summary>
    public InstallationInfo? GetInstallationInfo(string siteName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(siteName)) return null;
            using var siteKey = Registry.LocalMachine.OpenSubKey($"{BaseRegistryPath}\\{siteName}");
            if (siteKey == null) return null;

            var version = siteKey.GetValue(VersionKey)?.ToString() ?? siteKey.GetValue(InstalledVersionKey)?.ToString() ?? "Unknown";
            var installPath = siteKey.GetValue(InstallPathKey)?.ToString() ?? "Unknown";
            var installDate = siteKey.GetValue(InstallDateKey)?.ToString() ?? "Unknown";

            return new InstallationInfo
            {
                SiteName = siteName,
                Version = version,
                InstallPath = installPath,
                InstallDate = installDate
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get all detected installations
    /// </summary>
    public List<InstallationInfo> GetInstallations()
    {
        var list = new List<InstallationInfo>();
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(BaseRegistryPath);
            if (baseKey == null) return list;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                var info = GetInstallationInfo(subKeyName);
                if (info != null) list.Add(info);
            }
        }
        catch
        {
            // ignore and return what we have
        }
        return list;
    }

    /// <summary>
    /// Save installation information to per-instance registry (
    /// HKLM\SOFTWARE\EcommerceStarter\{SiteName}).
    /// Note: Programs & Features entry is created by InstallationService
    /// </summary>
    public bool SaveInstallationInfo(string version, string installPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(installPath)) return false;
            var siteName = Path.GetFileName(installPath.TrimEnd('\\', '/'));
            if (string.IsNullOrWhiteSpace(siteName)) return false;

            using var key = Registry.LocalMachine.CreateSubKey($"{BaseRegistryPath}\\{siteName}");
            if (key == null) return false;

            key.SetValue(VersionKey, version);
            key.SetValue(InstallPathKey, installPath);
            key.SetValue(InstallDateKey, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Remove installation information for a specific site from registry
    /// </summary>
    public bool RemoveInstallationInfo(string siteName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(siteName)) return false;
            Registry.LocalMachine.DeleteSubKeyTree($"{BaseRegistryPath}\\{siteName}", false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class InstallationInfo
{
    public string SiteName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
}
