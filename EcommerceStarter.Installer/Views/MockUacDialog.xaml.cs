using System.Windows;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Mock UAC (User Account Control) dialog for demo purposes
/// </summary>
public partial class MockUacDialog : Window
{
    public MockUacDialog(string appName, string publisher = "Microsoft Corporation")
    {
        InitializeComponent();
        AppNameText.Text = appName;
        PublisherText.Text = publisher;
    }
    
    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
    
    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
