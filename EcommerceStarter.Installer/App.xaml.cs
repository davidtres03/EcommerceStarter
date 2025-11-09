using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using EcommerceStarter.Installer.Models;
using EcommerceStarter.Installer.Views;

namespace EcommerceStarter.Installer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Win32 API for more reliable keyboard state detection
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    
    private const int VK_SHIFT = 0x10;
    
    public static bool IsDebugMode { get; private set; } = false;
    public static bool IsMockExistingMode { get; private set; } = false;
    public static bool IsUninstallMode { get; private set; } = false;
    public static bool IsDemoMode { get; set; } = false;
    public static DemoScenario CurrentDemoScenario { get; set; } = DemoScenario.Selection;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // SECRET DEMO MODE: Hold Shift while launching!
        // Use Win32 API for reliable detection
        bool shiftHeld = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
        
        // Also check if --shift-demo flag was passed (from elevated restart)
        bool demoFlagPassed = e.Args.Contains("--shift-demo");
        bool isDemoModeRequested = shiftHeld || demoFlagPassed;
        
        // Demo mode FIRST - doesn't require admin!
        if (isDemoModeRequested)
        {
            // Easter egg activated! Show beautiful demo launcher
            // Demo mode is SAFE and doesn't need admin privileges
            IsDemoMode = true;
            var demoLauncher = new DemoLauncherWindow();
            demoLauncher.Show();
            return; // Don't continue with normal startup - no admin check needed!
        }

        // Check for help/version flags (don't require admin)
        if (e.Args.Contains("--help") || e.Args.Contains("-h"))
        {
            ShowHelp();
            Current.Shutdown();
            return;
        }
        
        if (e.Args.Contains("--version") || e.Args.Contains("-v"))
        {
            ShowVersion();
            Current.Shutdown();
            return;
        }
        
        // Check for demo mode (doesn't require admin - safe mode)
        if (e.Args.Contains("--demo") || e.Args.Any(arg => arg.StartsWith("--demo-")))
        {
            IsDemoMode = true;
            CurrentDemoScenario = DetermineDemoScenario(e.Args);
            
            if (CurrentDemoScenario == DemoScenario.Selection)
            {
                // Show demo selection window
                var demoSelectionWindow = new DemoSelectionWindow();
                demoSelectionWindow.Show();
            }
            else
            {
                // Direct to specific demo scenario
                var mainWindow = new MainWindow();
                mainWindow.LaunchDemoScenario(CurrentDemoScenario);
                mainWindow.Show();
            }
            return;
        }

        // Check if running as administrator (required for production mode)
        if (!IsAdministrator())
        {
            // If Shift was held, we need to pass --shift-demo flag when restarting
            var additionalArgs = isDemoModeRequested ? " --shift-demo" : "";
            
            var result = MessageBox.Show(
                "EcommerceStarter Installer requires administrator privileges to:\n\n" +
                "• Install IIS components\n" +
                "• Create IIS websites\n" +
                "• Modify system registry\n" +
                "• Install prerequisites\n\n" +
                "Would you like to restart the installer with administrator privileges?",
                "Administrator Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Restart with elevation
                try
                {
                    var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                    exePath = exePath.Replace(".dll", ".exe");

                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas", // This triggers UAC
                        Arguments = string.Join(" ", e.Args) + additionalArgs
                    };

                    System.Diagnostics.Process.Start(processInfo);
                    
                    // Exit immediately to prevent duplicate instances
                    Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to restart with administrator privileges:\n\n{ex.Message}\n\n" +
                        "Please right-click the installer and select 'Run as administrator'.",
                        "Elevation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show(
                    "The installer will continue, but some operations may fail without administrator privileges.\n\n" +
                    "If you encounter errors, please restart the installer as administrator.",
                    "Limited Functionality",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // Continue without admin - let user proceed but warn them
                return;
            }

            // This return is now unreachable since we exit above, but kept for clarity
            return;
        }

        // Continue with normal startup - already admin
        
        // Check for installer updates (async - don't block startup)
        CheckForInstallerUpdatesAsync(e.Args);
        
        // Check for command-line arguments (mock mode, uninstall, etc.)
        if (e.Args.Contains("--uninstall") || e.Args.Contains("-u"))
        {
            IsUninstallMode = true;
            
            // Launch uninstall window
            try
            {
                var uninstallWindow = new UninstallWindow();
                uninstallWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch uninstaller:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Uninstaller Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
        else
        {
            // Check for mock mode
            IsDebugMode = e.Args.Contains("-MockExisting") || e.Args.Contains("--mock");
            
            // Launch main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
    
    /// <summary>
    /// Check for installer updates in background
    /// </summary>
    private async void CheckForInstallerUpdatesAsync(string[] args)
    {
        try
        {
            // Skip update check if --no-update flag is present
            if (args.Contains("--no-update"))
                return;
            
            var updateService = new Services.UpdateService();
            var updateInfo = await updateService.CheckForUpdatesAsync();
            
            if (updateInfo != null && updateInfo.HasUpdate)
            {
                // Show update available dialog
                var result = MessageBox.Show(
                    $"Installer Update Available!\n\n" +
                    $"Current Version: {updateInfo.CurrentVersion}\n" +
                    $"Latest Version: {updateInfo.LatestVersion}\n" +
                    $"Released: {updateInfo.PublishedAt:yyyy-MM-dd}\n\n" +
                    $"What's New:\n{updateInfo.ReleaseNotes.Substring(0, Math.Min(200, updateInfo.ReleaseNotes.Length))}...\n\n" +
                    $"Would you like to download and install the update now?\n" +
                    $"(The installer will restart with the new version)",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Show progress dialog
                    MessageBox.Show(
                        "Downloading update...\n\nThe installer will restart automatically.",
                        "Updating",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    // Download and apply update
                    await updateService.DownloadAndApplyInstallerUpdateAsync(updateInfo.DownloadUrl);
                    
                    // If we get here, update failed
                    MessageBox.Show(
                        "Update failed. Please download the latest version manually from GitHub.",
                        "Update Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }
        catch
        {
            // Update check failed - ignore and continue
        }
    }

    private bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
    
    private void ShowHelp()
    {
        var helpText = @"╔═══════════════════════════════════════════════════════════╗
║  EcommerceStarter Installer - Command-Line Options       ║
╚═══════════════════════════════════════════════════════════╝

PRODUCTION MODE:
  (no flags)              Launch installer normally
                          • Auto-detects existing installation
                          • Shows maintenance or install wizard

DEMO MODE (Safe - No Changes):
  --demo                  Show demo mode selection screen
  --demo-fresh            Demo: Fresh installation
  --demo-upgrade          Demo: Upgrade existing
  --demo-reconfig         Demo: Reconfigure settings
  --demo-repair           Demo: Repair installation
  --demo-uninstall        Demo: Uninstall

UTILITY:
  --uninstall, -u         Launch uninstaller directly
  --help, -h              Show this help
  --version, -v           Show version info

EXAMPLES:
  EcommerceStarter.Installer.exe
    → Normal installation flow

  EcommerceStarter.Installer.exe --demo
    → Show all demo scenarios (safe, no changes)

  EcommerceStarter.Installer.exe --demo-upgrade
    → Directly demonstrate upgrade workflow

  EcommerceStarter.Installer.exe --uninstall
    → Launch uninstaller

For more information, visit:
https://github.com/yourusername/EcommerceStarter";
        
        MessageBox.Show(helpText, "EcommerceStarter Installer Help", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void ShowVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = System.IO.File.GetLastWriteTime(assembly.Location);
        
        var versionText = $@"EcommerceStarter Installer

Version: {version}
Build Date: {buildDate:yyyy-MM-dd HH:mm:ss}
.NET: {Environment.Version}
OS: {Environment.OSVersion}

Copyright © 2025 David Thomas Resnick
Licensed under MIT License";
        
        MessageBox.Show(versionText, "Version Information",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private DemoScenario DetermineDemoScenario(string[] args)
    {
        if (args.Contains("--demo-fresh")) return DemoScenario.FreshInstall;
        if (args.Contains("--demo-upgrade")) return DemoScenario.Upgrade;
        if (args.Contains("--demo-reconfig")) return DemoScenario.Reconfigure;
        if (args.Contains("--demo-repair")) return DemoScenario.Repair;
        if (args.Contains("--demo-uninstall")) return DemoScenario.Uninstall;
        
        return DemoScenario.Selection; // Show selection screen
    }
}

