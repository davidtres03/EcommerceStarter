using System;
using System.IO;

namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Mock installation state for testing/demo purposes
/// Saved to disk to persist between installer runs
/// </summary>
public class MockInstallationState
{
    // Company/Store Information
    public string CompanyName { get; set; } = "My Store";
    public string SiteTagline { get; set; } = "Your trusted online marketplace";
    
    // Admin Account
    public string AdminEmail { get; set; } = "admin@mystore.com";
    // Note: Never save passwords, even in mock data!
    
    // Database Configuration
    public string DatabaseServer { get; set; } = "localhost\\SQLEXPRESS";
    public string DatabaseName { get; set; } = "MyStoreDB";
    
    // Installation Metadata
    public string InstallPath { get; set; } = @"C:\inetpub\EcommerceStarter";
    public string InstallDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string Version { get; set; } = "1.0.0";
    
    // Optional Features
    public bool StripeConfigured { get; set; } = false;
    public string? StripePublishableKey { get; set; }
    
    public bool EmailConfigured { get; set; } = false;
    public string EmailProvider { get; set; } = "None";
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    
    // Helpers
    public static string GetMockStateFilePath()
    {
        // Save in the executable directory
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(exePath, "mock-state.json");
    }
}
