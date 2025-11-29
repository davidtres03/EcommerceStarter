using System;
using Microsoft.Win32;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for detecting and managing installation state
/// </summary>
public class InstallationStateService
{
    private const string RegistryPath = @"SOFTWARE\EcommerceStarter";
    private const string InstalledVersionKey = "InstalledVersion";
    private const string InstallPathKey = "InstallPath";
    private const string InstallDateKey = "InstallDate";
    
    /// <summary>
    /// Check if EcommerceStarter is already installed
    /// </summary>
    public bool IsInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            return key != null && !string.IsNullOrEmpty(key.GetValue(InstalledVersionKey)?.ToString());
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Get installation information
    /// </summary>
    public InstallationInfo? GetInstallationInfo()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            if (key == null) return null;
            
            return new InstallationInfo
            {
                Version = key.GetValue(InstalledVersionKey)?.ToString() ?? "Unknown",
                InstallPath = key.GetValue(InstallPathKey)?.ToString() ?? "Unknown",
                InstallDate = key.GetValue(InstallDateKey)?.ToString() ?? "Unknown"
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Save installation information to registry (custom tracking only)
    /// Note: Programs & Features entry is created by InstallationService
    /// </summary>
    public bool SaveInstallationInfo(string version, string installPath)
    {
        try
        {
            // Save to our custom registry location for tracking
            using var key = Registry.LocalMachine.CreateSubKey(RegistryPath);
            if (key == null) return false;
            
            key.SetValue(InstalledVersionKey, version);
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
    /// Remove installation information from registry
    /// </summary>
    public bool RemoveInstallationInfo()
    {
        try
        {
            // Remove our custom registry location
            Registry.LocalMachine.DeleteSubKeyTree(RegistryPath, false);
            
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
    public string Version { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
}
