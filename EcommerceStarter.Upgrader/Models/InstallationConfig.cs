using System;
using System.ComponentModel;

namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Installation configuration settings collected from the wizard
/// </summary>
public class InstallationConfig : INotifyPropertyChanged
{
    // Company Information
    public string CompanyName { get; set; } = "My Store";
    public string SiteTagline { get; set; } = "Powered by EcommerceStarter";

    // Admin Account
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    // Database Configuration
    public string DatabaseServer { get; set; } = "localhost\\SQLEXPRESS";
    public string DatabaseName { get; set; } = "MyStore";

    // Website Configuration
    public string SiteName { get; set; } = "MyStore";
    public string Domain { get; set; } = "localhost";
    public int Port { get; set; } = 443;

    // Optional Features
    public bool ConfigureStripe { get; set; } = false;
    public string? StripePublishableKey { get; set; }
    public string? StripeSecretKey { get; set; }

    public bool ConfigureEmail { get; set; } = false;
    public EmailProvider EmailProvider { get; set; } = EmailProvider.None;
    public string? EmailApiKey { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }

    // Installation Paths
    public string InstallationPath { get; set; } = @"C:\inetpub\EcommerceStarter";

    // Installation State
    public bool IsExistingInstallation { get; set; } = false;
    public bool IsReconfiguration { get; set; } = false;
    public string? ExistingInstallationPath { get; set; }
    public DateTime? ExistingInstallationDate { get; set; }

    // Database Mode
    public bool UseExistingDatabase { get; set; } = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum EmailProvider
{
    None,
    Resend,
    Smtp
}
