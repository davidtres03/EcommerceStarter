using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace EcommerceStarter.Installer.Helpers;

/// <summary>
/// Helper class for opening documentation files
/// </summary>
public static class DocumentationHelper
{
    private static readonly string DocsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        "..", "..", "..", "..", "..", 
        "docs", "setup-guides");

    /// <summary>
    /// Open the Stripe setup guide
    /// </summary>
    public static void OpenStripeGuide()
    {
        OpenMarkdownFile("STRIPE_SETUP.md", "https://github.com/yourusername/EcommerceStarter/blob/main/docs/setup-guides/STRIPE_SETUP.md");
    }

    /// <summary>
    /// Open the Resend email setup guide
    /// </summary>
    public static void OpenResendGuide()
    {
        OpenMarkdownFile("EMAIL_RESEND_SETUP.md", "https://github.com/yourusername/EcommerceStarter/blob/main/docs/setup-guides/EMAIL_RESEND_SETUP.md");
    }

    /// <summary>
    /// Open the SMTP email setup guide
    /// </summary>
    public static void OpenSmtpGuide()
    {
        OpenMarkdownFile("EMAIL_SMTP_SETUP.md", "https://github.com/yourusername/EcommerceStarter/blob/main/docs/setup-guides/EMAIL_SMTP_SETUP.md");
    }

    /// <summary>
    /// Open the main setup guides index
    /// </summary>
    public static void OpenSetupGuidesIndex()
    {
        OpenMarkdownFile("README.md", "https://github.com/yourusername/EcommerceStarter/blob/main/docs/setup-guides/README.md");
    }

    /// <summary>
    /// Open a markdown file in the default viewer or browser
    /// </summary>
    private static void OpenMarkdownFile(string filename, string fallbackUrl)
    {
        try
        {
            // Try to find the local file
            var localPath = Path.GetFullPath(Path.Combine(DocsPath, filename));
            
            if (File.Exists(localPath))
            {
                // Open with default markdown viewer or text editor
                Process.Start(new ProcessStartInfo
                {
                    FileName = localPath,
                    UseShellExecute = true
                });
                
                Debug.WriteLine($"[Documentation] Opened local file: {localPath}");
            }
            else
            {
                // Fallback to online version
                Debug.WriteLine($"[Documentation] Local file not found: {localPath}");
                Debug.WriteLine($"[Documentation] Opening online version: {fallbackUrl}");
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = fallbackUrl,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Documentation] Error opening documentation: {ex.Message}");
            
            // Show user-friendly message
            MessageBox.Show(
                $"Unable to open documentation file.\n\n" +
                $"Please visit:\n{fallbackUrl}\n\n" +
                $"Or check the docs/setup-guides/ folder in your installation directory.",
                "Documentation",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Show a quick help tooltip/popup
    /// </summary>
    public static void ShowQuickHelp(string title, string message)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Get quick help text for Stripe configuration
    /// </summary>
    public static string GetStripeQuickHelp()
    {
        return "?? Stripe Payment Setup Quick Guide:\n\n" +
               "1. Go to https://stripe.com and sign up\n" +
               "2. Navigate to Developers ? API Keys\n" +
               "3. Copy your Publishable Key (pk_test_...)\n" +
               "4. Copy your Secret Key (sk_test_...)\n" +
               "5. Click 'Validate Keys' to test\n\n" +
               "?? Use TEST keys for development\n" +
               "?? Switch to LIVE keys before production\n\n" +
               "Click 'Setup Guide' for detailed instructions.";
    }

    /// <summary>
    /// Get quick help text for Resend configuration
    /// </summary>
    public static string GetResendQuickHelp()
    {
        return "?? Resend Email Quick Guide:\n\n" +
               "1. Go to https://resend.com and sign up\n" +
               "2. Click 'API Keys' in the sidebar\n" +
               "3. Create a new API key\n" +
               "4. Choose 'Sending access' (recommended)\n" +
               "5. Copy the key (starts with re_)\n" +
               "6. Click 'Test API' to validate\n\n" +
               "? Free tier: 100 emails/day\n" +
               "?? Restricted keys are more secure!\n\n" +
               "Click 'Setup Guide' for detailed instructions.";
    }

    /// <summary>
    /// Get quick help text for SMTP configuration
    /// </summary>
    public static string GetSmtpQuickHelp()
    {
        return "?? SMTP Email Quick Guide:\n\n" +
               "Gmail:\n" +
               "  Host: smtp.gmail.com\n" +
               "  Port: 587\n" +
               "  ?? Use App Password (not your regular password!)\n\n" +
               "Outlook:\n" +
               "  Host: smtp-mail.outlook.com\n" +
               "  Port: 587\n\n" +
               "SendGrid:\n" +
               "  Host: smtp.sendgrid.net\n" +
               "  Port: 587\n" +
               "  Username: apikey\n\n" +
               "Click 'Setup Guide' for provider-specific instructions.";
    }
}
