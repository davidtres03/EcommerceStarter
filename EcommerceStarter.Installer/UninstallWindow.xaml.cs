using System.Windows;
using EcommerceStarter.Installer.Views;

namespace EcommerceStarter.Installer;

public partial class UninstallWindow : Window
{
    public UninstallWindow()
    {
        InitializeComponent();
        
        // Navigate to uninstall page
        var uninstallPage = new UninstallPage();
        MainFrame.Navigate(uninstallPage);
    }
}
