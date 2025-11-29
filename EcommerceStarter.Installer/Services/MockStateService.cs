using System;
using System.IO;
using System.Text.Json;
using EcommerceStarter.Installer.Models;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Manages persistent mock installation state for testing/demo
/// </summary>
public static class MockStateService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    
    /// <summary>
    /// Save configuration as mock installation state
    /// </summary>
    public static bool SaveMockState(InstallationConfig config)
    {
        try
        {
            var mockState = new MockInstallationState
            {
                CompanyName = config.CompanyName,
                SiteTagline = config.SiteTagline,
                AdminEmail = config.AdminEmail,
                DatabaseServer = config.DatabaseServer,
                DatabaseName = config.DatabaseName,
                InstallPath = config.InstallationPath,
                InstallDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Version = "1.0.0",
                StripeConfigured = config.ConfigureStripe,
                StripePublishableKey = config.StripePublishableKey,
                EmailConfigured = config.ConfigureEmail,
                EmailProvider = config.EmailProvider.ToString(),
                SmtpHost = config.SmtpHost,
                SmtpPort = config.SmtpPort
            };
            
            var filePath = MockInstallationState.GetMockStateFilePath();
            var json = JsonSerializer.Serialize(mockState, _jsonOptions);
            File.WriteAllText(filePath, json);
            
            System.Diagnostics.Debug.WriteLine($"[MockStateService] Saved mock state to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MockStateService] Failed to save mock state: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load mock installation state if it exists
    /// </summary>
    public static MockInstallationState? LoadMockState()
    {
        try
        {
            var filePath = MockInstallationState.GetMockStateFilePath();
            
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine("[MockStateService] No mock state file found");
                return null;
            }
            
            var json = File.ReadAllText(filePath);
            var mockState = JsonSerializer.Deserialize<MockInstallationState>(json, _jsonOptions);
            
            System.Diagnostics.Debug.WriteLine($"[MockStateService] Loaded mock state from: {filePath}");
            return mockState;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MockStateService] Failed to load mock state: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Check if mock state file exists
    /// </summary>
    public static bool MockStateExists()
    {
        var filePath = MockInstallationState.GetMockStateFilePath();
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Delete mock state file (for testing clean slate)
    /// </summary>
    public static bool DeleteMockState()
    {
        try
        {
            var filePath = MockInstallationState.GetMockStateFilePath();
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                System.Diagnostics.Debug.WriteLine($"[MockStateService] Deleted mock state: {filePath}");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MockStateService] Failed to delete mock state: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get default mock state (used as fallback)
    /// </summary>
    public static MockInstallationState GetDefaultMockState()
    {
        return new MockInstallationState
        {
            CompanyName = "My Store",
            SiteTagline = "Your trusted online marketplace",
            AdminEmail = "admin@mystore.com",
            DatabaseServer = "localhost\\SQLEXPRESS",
            DatabaseName = "EcommerceStarter",
            InstallPath = @"C:\inetpub\EcommerceStarter",
            InstallDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss"),
            Version = "1.0.0",
            StripeConfigured = true,
            StripePublishableKey = "pk_test_51ABC***hidden***"
        };
    }
}
