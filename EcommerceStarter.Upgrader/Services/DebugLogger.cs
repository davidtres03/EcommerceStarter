using System;
using System.IO;
using System.Text;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Helper class for file-based logging to troubleshoot issues
/// </summary>
public static class DebugLogger
{
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EcommerceStarter",
        "Logs",
        $"installer-debug-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log"
    );

    static DebugLogger()
    {
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch { }
    }

    public static void Log(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var fullMessage = $"[{timestamp}] {message}";

            // Write to Debug output
            System.Diagnostics.Debug.WriteLine(fullMessage);

            // Write to file with retry logic
            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    File.AppendAllText(_logPath, fullMessage + Environment.NewLine);
                    System.Diagnostics.Debug.WriteLine($"[DebugLogger] Successfully wrote to {_logPath}");
                    break;
                }
                catch (IOException)
                {
                    retries--;
                    if (retries > 0)
                    {
                        System.Threading.Thread.Sleep(100); // Wait 100ms before retry
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DebugLogger] Failed to write to {_logPath} after retries");
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DebugLogger] EXCEPTION in Log(): {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void LogException(Exception ex, string context = "")
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[EXCEPTION] {context}");
            sb.AppendLine($"  Type: {ex.GetType().FullName}");
            sb.AppendLine($"  Message: {ex.Message}");
            sb.AppendLine($"  Source: {ex.Source}");
            sb.AppendLine($"  StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"  InnerException Type: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"  InnerException Message: {ex.InnerException.Message}");
                sb.AppendLine($"  InnerException StackTrace: {ex.InnerException.StackTrace}");
            }

            Log(sb.ToString());
        }
        catch { }
    }

    public static string GetLogPath() => _logPath;
}