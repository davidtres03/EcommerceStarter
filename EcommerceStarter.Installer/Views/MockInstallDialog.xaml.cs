using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Mock installation progress dialog for demo purposes
/// </summary>
public partial class MockInstallDialog : Window
{
    private DispatcherTimer _timer;
    private int _progress = 0;
    
    public MockInstallDialog(string appName, string status = "Installing...")
    {
        InitializeComponent();
        AppNameText.Text = appName;
        StatusText.Text = status;
        
        // Simulate progress
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }
    
    private void Timer_Tick(object? sender, EventArgs e)
    {
        _progress += 2;
        ProgressBar.Value = _progress;
        
        if (_progress >= 100)
        {
            _timer.Stop();
            Task.Delay(500).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    DialogResult = true;
                    Close();
                });
            });
        }
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = false;
        Close();
    }
}
