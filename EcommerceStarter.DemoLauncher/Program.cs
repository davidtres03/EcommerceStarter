using System;
using System.Windows;
using EcommerceStarter.Installer.Views;

namespace EcommerceStarter.DemoLauncher;

/// <summary>
/// Beautiful standalone demo launcher application
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new Application();
        var window = new DemoLauncherWindow();
        app.Run(window);
    }
}
