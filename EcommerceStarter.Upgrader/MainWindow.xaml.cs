using System;
using System.Linq;
using System.Windows;
using EcommerceStarter.Upgrader.Views;
using EcommerceStarter.Upgrader.Models;
using EcommerceStarter.Upgrader.Services;

namespace EcommerceStarter.Upgrader;

/// <summary>
/// Upgrade application main window - launches directly to upgrade flow
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[Upgrader.MainWindow] === UPGRADER STARTED ===");

            // Parse command-line arguments to get existing installation details
            var args = Environment.GetCommandLineArgs();
            System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow] Total args count: {args.Length}");
            for (int i = 0; i < args.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow]   args[{i}]: '{args[i]}'");
            }

            var existingInstall = ParseInstallationFromArgs(args);

            if (existingInstall == null)
            {
                MessageBox.Show(
                    "ERROR: The upgrader must be launched by the installer with installation details.\n\n" +
                    "Please run the EcommerceStarter.Installer.exe and choose 'Upgrade' from the maintenance menu.",
                    "Upgrader Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Application.Current.Shutdown();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow] Parsed installation: {existingInstall.SiteName} v{existingInstall.Version}");

            // Launch upgrade welcome page directly
            var welcomePage = new UpgradeWelcomePage(existingInstall);
            ContentFrame.Navigate(welcomePage);

            System.Diagnostics.Debug.WriteLine("[Upgrader.MainWindow] Navigated to UpgradeWelcomePage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow] FATAL ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow] Stack trace: {ex.StackTrace}");

            MessageBox.Show(
                $"Fatal error starting upgrader:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Upgrader Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Parse ExistingInstallation from command-line arguments
    /// Expected format: --sitename "CapAndCollarSupplyCo" --installpath "C:\inetpub\..." --dbserver "localhost" --dbname "EcommerceSt" --version "1.0.9.47" --productcount 0 --ordercount 0 --usercount 0
    /// </summary>
    private ExistingInstallation? ParseInstallationFromArgs(string[] args)
    {
        try
        {
            string? siteName = null;
            string? installPath = null;
            string? dbServer = null;
            string? dbName = null;
            string? version = null;
            int productCount = 0;
            int orderCount = 0;
            int userCount = 0;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLower();
                
                if ((arg == "--sitename" || arg == "-s") && i + 1 < args.Length)
                {
                    siteName = args[++i].Trim('"');
                }
                else if ((arg == "--installpath" || arg == "-i") && i + 1 < args.Length)
                {
                    installPath = args[++i].Trim('"');
                }
                else if ((arg == "--dbserver" || arg == "-ds") && i + 1 < args.Length)
                {
                    dbServer = args[++i].Trim('"');
                }
                else if ((arg == "--dbname" || arg == "-dn") && i + 1 < args.Length)
                {
                    dbName = args[++i].Trim('"');
                }
                else if ((arg == "--version" || arg == "-v") && i + 1 < args.Length)
                {
                    version = args[++i].Trim('"');
                }
                else if (arg == "--productcount" && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out productCount);
                }
                else if (arg == "--ordercount" && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out orderCount);
                }
                else if (arg == "--usercount" && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out userCount);
                }
            }

            // Validate required fields
            if (string.IsNullOrEmpty(siteName) || string.IsNullOrEmpty(installPath) || 
                string.IsNullOrEmpty(dbServer) || string.IsNullOrEmpty(dbName))
            {
                System.Diagnostics.Debug.WriteLine("[Upgrader.MainWindow] Missing required arguments:");
                System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow]   siteName: '{siteName ?? "NULL"}'");
                System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow]   installPath: '{installPath ?? "NULL"}'");
                System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow]   dbServer: '{dbServer ?? "NULL"}'");
                System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow]   dbName: '{dbName ?? "NULL"}'");
                System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow]   version: '{version ?? "NULL"}'");
                return null;
            }

            // Create ExistingInstallation object
            return new ExistingInstallation
            {
                SiteName = siteName,
                CompanyName = siteName, // Use siteName as company name
                InstallPath = installPath,
                DatabaseServer = dbServer,
                DatabaseName = dbName,
                Version = version ?? "Unknown",
                IsHealthy = true, // Assume healthy if launching from installer
                ProductCount = productCount,
                OrderCount = orderCount,
                UserCount = userCount,
                Issues = null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Upgrader.MainWindow] Error parsing args: {ex.Message}");
            return null;
        }
    }
}
