using System;
using System.Windows;
using EcommerceStarter.Upgrader.Views;
using EcommerceStarter.Upgrader.Models;

namespace EcommerceStarter.Upgrader;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Stub properties for compatibility - upgrader doesn't support these modes
    public static bool IsDebugMode { get; set; } = false;
    public static bool IsDemoMode { get; set; } = false;
    public static DemoScenario CurrentDemoScenario { get; set; } = DemoScenario.Selection;


    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers for debugging
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[App.OnStartup] UNHANDLED EXCEPTION: {ex?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[App.OnStartup] Message: {ex?.Message}");
            System.Diagnostics.Debug.WriteLine($"[App.OnStartup] Stack: {ex?.StackTrace}");
        };

        Current.DispatcherUnhandledException += (s, args) =>
        {
            var ex = args.Exception;
            System.Diagnostics.Debug.WriteLine($"[App.DispatcherUnhandledException] DISPATCHER EXCEPTION: {ex?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[App.DispatcherUnhandledException] Message: {ex?.Message}");
            System.Diagnostics.Debug.WriteLine($"[App.DispatcherUnhandledException] Stack: {ex?.StackTrace}");
            args.Handled = false; // Let it propagate for debugging
        };

        System.Diagnostics.Debug.WriteLine("[App.OnStartup] === UPGRADER STARTUP ===");

        // Launch main window directly - upgrader is dedicated to upgrade only
        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App.OnStartup] MAIN WINDOW ERROR: {ex.Message}");
            MessageBox.Show(
                $"Failed to launch upgrader:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Upgrader Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Current.Shutdown();
        }
    }
}
