using System.Windows;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Mock success dialog for demo purposes
/// </summary>
public partial class MockSuccessDialog : Window
{
    public MockSuccessDialog(string message, string title = "Installation Complete")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
