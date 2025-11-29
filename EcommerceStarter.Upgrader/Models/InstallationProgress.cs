using System.ComponentModel;

namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Tracks installation progress and status
/// </summary>
public class InstallationProgress : INotifyPropertyChanged
{
    private string _currentStep = string.Empty;
    private int _progressPercentage = 0;
    private string _statusMessage = string.Empty;
    private bool _isIndeterminate = false;
    private bool _hasError = false;
    private string _errorMessage = string.Empty;
    
    public string CurrentStep
    {
        get => _currentStep;
        set
        {
            _currentStep = value;
            OnPropertyChanged(nameof(CurrentStep));
        }
    }
    
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            _progressPercentage = value;
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }
    
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set
        {
            _isIndeterminate = value;
            OnPropertyChanged(nameof(IsIndeterminate));
        }
    }
    
    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged(nameof(HasError));
        }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public void UpdateProgress(string step, int percentage, string message)
    {
        CurrentStep = step;
        ProgressPercentage = percentage;
        StatusMessage = message;
        IsIndeterminate = false;
        HasError = false;
    }
    
    public void SetError(string error)
    {
        HasError = true;
        ErrorMessage = error;
        StatusMessage = "Installation failed";
    }
    
    public void SetIndeterminate(string step, string message)
    {
        CurrentStep = step;
        StatusMessage = message;
        IsIndeterminate = true;
        HasError = false;
    }
}
