using System;
using System.IO;
using System.Text;
using System.Threading;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Centralized logging service that writes to both UI and file
/// Ensures all upgrade/installation logs are captured and accessible
/// </summary>
public class LoggerService
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly object _lockObject = new();
    
    /// <summary>
    /// Event fired when a new log entry is written
    /// UI subscribers can update the display in real-time
    /// </summary>
    public event EventHandler<string>? LogEntryAdded;

    public LoggerService()
    {
        // Create logs directory in AppData\Local\EcommerceStarter\Logs
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EcommerceStarter",
            "Logs"
        );
        
        Directory.CreateDirectory(_logDirectory);
        
        // Create log file with timestamp
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logFilePath = Path.Combine(_logDirectory, $"installer_{timestamp}.log");
        
        // Initialize log file with header
        WriteToFile($"=== EcommerceStarter Installation Log ===");
        WriteToFile($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        WriteToFile($"Log File: {_logFilePath}");
        WriteToFile($"User: {Environment.UserName}");
        WriteToFile($"Computer: {Environment.MachineName}");
        WriteToFile($".NET Version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        WriteToFile("");
    }

    /// <summary>
    /// Write a log entry to both UI (via event) and file
    /// Thread-safe to handle concurrent logging from multiple services
    /// </summary>
    public void Log(string message)
    {
        lock (_lockObject)
        {
            try
            {
                // Add timestamp to message
                var timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                
                // Write to file
                WriteToFile(timestampedMessage);
                
                // Raise event for UI subscribers
                LogEntryAdded?.Invoke(this, timestampedMessage);
            }
            catch (Exception ex)
            {
                // If logging fails, at least try to fire the event so UI knows something happened
                try
                {
                    LogEntryAdded?.Invoke(this, $"[LOGGING ERROR] {ex.Message}");
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Log an error with exception details
    /// </summary>
    public void LogError(string message, Exception? ex = null)
    {
        var errorMessage = ex != null 
            ? $"ERROR: {message} - {ex.GetType().Name}: {ex.Message}" 
            : $"ERROR: {message}";
        
        Log(errorMessage);
        
        if (ex?.InnerException != null)
        {
            Log($"  Inner: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// Log a warning
    /// </summary>
    public void LogWarning(string message)
    {
        Log($"WARN: {message}");
    }

    /// <summary>
    /// Log with section header for organization
    /// </summary>
    public void LogSection(string sectionName)
    {
        Log("");
        Log($"=== {sectionName} ===");
    }

    /// <summary>
    /// Write directly to log file (internal use)
    /// </summary>
    private void WriteToFile(string message)
    {
        try
        {
            File.AppendAllText(_logFilePath, message + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // If file write fails, we can't do much about it
            // Don't throw - let the operation continue
        }
    }

    /// <summary>
    /// Get the log file path (useful for showing user where logs are stored)
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// Get the log directory path
    /// </summary>
    public string LogDirectory => _logDirectory;

    /// <summary>
    /// Close the logger and finalize the log file
    /// </summary>
    public void Close()
    {
        lock (_lockObject)
        {
            WriteToFile("");
            WriteToFile($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteToFile("=== Installation Log Complete ===");
        }
    }
}
