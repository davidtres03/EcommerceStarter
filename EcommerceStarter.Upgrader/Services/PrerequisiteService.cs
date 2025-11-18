using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for checking and installing prerequisites
/// </summary>
public class PrerequisiteService
{
    private readonly HttpClient _httpClient = new();
    
    public event EventHandler<string>? StatusUpdate;
    public event EventHandler<int>? ProgressUpdate;
    
    // Download URLs
    private const string DotNetSdkUrl = "https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-8.0.101-windows-x64-installer";
    private const string SqlExpressUrl = "https://go.microsoft.com/fwlink/p/?linkid=2216019";
    
    /// <summary>
    /// Check if .NET 8 SDK is installed
    /// </summary>
    public async Task<bool> IsDotNetInstalledAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            // Check if version is 8 or higher
            if (string.IsNullOrWhiteSpace(output))
                return false;
                
            var versionParts = output.Trim().Split('.');
            if (versionParts.Length > 0 && int.TryParse(versionParts[0], out var majorVersion))
            {
                return majorVersion >= 8;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Install .NET 8 SDK
    /// </summary>
    public async Task<bool> InstallDotNetAsync()
    {
        try
        {
            StatusUpdate?.Invoke(this, "Downloading .NET 8 SDK...");
            
            var installerPath = Path.Combine(Path.GetTempPath(), "dotnet-sdk-8-installer.exe");
            
            // Download installer
            using var response = await _httpClient.GetAsync(DotNetSdkUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var bytesRead = 0L;
            
            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[8192];
                int read;
                
                while ((read = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    bytesRead += read;
                    
                    if (totalBytes > 0)
                    {
                        var progress = (int)((bytesRead * 100) / totalBytes);
                        ProgressUpdate?.Invoke(this, progress);
                    }
                }
            }
            
            StatusUpdate?.Invoke(this, "Installing .NET 8 SDK...");
            
            // Run installer
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/quiet /norestart",
                UseShellExecute = true,
                Verb = "runas" // Run as admin
            });
            
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                // Cleanup
                try { File.Delete(installerPath); } catch { }
                
                StatusUpdate?.Invoke(this, ".NET 8 SDK installed successfully!");
                return process.ExitCode == 0;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Error installing .NET: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if SQL Server is installed and running
    /// </summary>
    public async Task<bool> IsSqlServerInstalledAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check for SQL Server services
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE Name LIKE 'MSSQL%'");
                var services = searcher.Get();
                
                return services.Count > 0;
            }
            catch
            {
                return false;
            }
        });
    }
    
    /// <summary>
    /// Check if SQL Server is running
    /// </summary>
    public async Task<bool> IsSqlServerRunningAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Service WHERE Name LIKE 'MSSQL%' AND State = 'Running'");
                var services = searcher.Get();
                
                return services.Count > 0;
            }
            catch
            {
                return false;
            }
        });
    }
    
    /// <summary>
    /// Install SQL Server Express
    /// </summary>
    public async Task<bool> InstallSqlServerAsync()
    {
        try
        {
            StatusUpdate?.Invoke(this, "Downloading SQL Server Express (this may take several minutes)...");
            
            var installerPath = Path.Combine(Path.GetTempPath(), "SQL2022-SSEI-Expr.exe");
            
            // Download installer
            using var response = await _httpClient.GetAsync(SqlExpressUrl);
            response.EnsureSuccessStatusCode();
            
            await using var fileStream = new FileStream(installerPath, FileMode.Create);
            await response.Content.CopyToAsync(fileStream);
            
            StatusUpdate?.Invoke(this, "Launching SQL Server Express installer...");
            StatusUpdate?.Invoke(this, "Please follow the installer prompts and use 'SQLEXPRESS' as the instance name.");
            
            // Launch installer (it's a bootstrapper, so we can't run silently)
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true,
                Verb = "runas"
            });
            
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                // Cleanup
                try { File.Delete(installerPath); } catch { }
                
                // Wait a bit for service to start
                await Task.Delay(10000);
                
                StatusUpdate?.Invoke(this, "SQL Server Express installation completed!");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Error installing SQL Server: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if IIS is installed and enabled
    /// </summary>
    public async Task<bool> IsIISInstalledAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check if IIS service exists
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Service WHERE Name = 'W3SVC'");
                var services = searcher.Get();
                
                return services.Count > 0;
            }
            catch
            {
                return false;
            }
        });
    }
    
    /// <summary>
    /// Install IIS using PowerShell
    /// </summary>
    public async Task<bool> InstallIISAsync()
    {
        try
        {
            StatusUpdate?.Invoke(this, "Installing IIS (this will take several minutes)...");
            
            var features = new[]
            {
                "IIS-WebServerRole",
                "IIS-WebServer",
                "IIS-CommonHttpFeatures",
                "IIS-ApplicationDevelopment",
                "IIS-NetFxExtensibility45",
                "IIS-HealthAndDiagnostics",
                "IIS-Security",
                "IIS-Performance",
                "IIS-WebServerManagementTools",
                "IIS-ManagementConsole",
                "IIS-StaticContent",
                "IIS-DefaultDocument",
                "IIS-ASPNET45"
            };
            
            var featureList = string.Join(",", features);
            var command = $"Enable-WindowsOptionalFeature -Online -FeatureName {featureList} -NoRestart -All";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            StatusUpdate?.Invoke(this, "IIS installed successfully!");
            StatusUpdate?.Invoke(this, "Note: A system restart may be required for all IIS features.");
            
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Error installing IIS: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if ASP.NET Core Hosting Bundle is installed
    /// </summary>
    public async Task<bool> IsHostingBundleInstalledAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Primary method: Check for IIS AspNetCore module (this is the key indicator)
                var modulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    @"IIS\Asp.Net Core Module\V2\aspnetcorev2.dll");
                if (File.Exists(modulePath))
                {
                    return true;
                }
                
                // Alternative module path
                var altModulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    @"IIS\Asp.Net Core Module\aspnetcore.dll");
                if (File.Exists(altModulePath))
                {
                    return true;
                }
                
                // Check registry for Hosting Bundle
                using (var view = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var uninstallKey = view.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                    {
                        if (uninstallKey != null)
                        {
                            var subKeyNames = uninstallKey.GetSubKeyNames();
                            foreach (var subKeyName in subKeyNames)
                            {
                                if (subKeyName.Contains("AspNetCoreHostingBundle") || 
                                    (subKeyName.Contains("DotNet") && subKeyName.Contains("Hosting")))
                                {
                                    using (var subKey = uninstallKey.OpenSubKey(subKeyName))
                                    {
                                        var displayName = subKey?.GetValue("DisplayName") as string;
                                        if (!string.IsNullOrEmpty(displayName) && 
                                            displayName.Contains("ASP.NET Core", StringComparison.OrdinalIgnoreCase) &&
                                            displayName.Contains("Hosting", StringComparison.OrdinalIgnoreCase))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Install ASP.NET Core Hosting Bundle
    /// </summary>
    public async Task<bool> InstallHostingBundleAsync()
    {
        try
        {
            StatusUpdate?.Invoke(this, "Downloading ASP.NET Core Hosting Bundle...");
            
            // Use Microsoft's permanent redirect URL that always points to the latest .NET 8.0 Hosting Bundle
            const string hostingBundleUrl = "https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe";
            
            var installerPath = Path.Combine(Path.GetTempPath(), "dotnet-hosting-bundle.exe");
            
            try
            {
                StatusUpdate?.Invoke(this, "Downloading from Microsoft CDN (this may take a moment)...");
                
                using var response = await _httpClient.GetAsync(hostingBundleUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesRead = 0L;
                
                StatusUpdate?.Invoke(this, $"Downloading {totalBytes / 1024 / 1024:F1} MB...");
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    int read;
                    
                    while ((read = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, read));
                        bytesRead += read;
                        
                        if (totalBytes > 0)
                        {
                            var progress = (int)((bytesRead * 100) / totalBytes);
                            ProgressUpdate?.Invoke(this, progress);
                            
                            if (progress % 10 == 0 && bytesRead > 0)
                            {
                                StatusUpdate?.Invoke(this, $"Downloading... {progress}%");
                            }
                        }
                    }
                }
                
                StatusUpdate?.Invoke(this, "Download complete!");
            }
            catch (HttpRequestException httpEx)
            {
                StatusUpdate?.Invoke(this, $"Download failed: {httpEx.Message}");
                StatusUpdate?.Invoke(this, "The installer will open your browser to download manually.");
                
                try
                {
                    var browserProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "https://dotnet.microsoft.com/en-us/download/dotnet/8.0",
                            UseShellExecute = true
                        }
                    };
                    browserProcess.Start();
                }
                catch { }
                
                return false;
            }
            
            StatusUpdate?.Invoke(this, "Installing ASP.NET Core Hosting Bundle...");
            StatusUpdate?.Invoke(this, "This may take a few minutes. Please wait...");
            
            var installProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/quiet /norestart OPT_NO_SHAREDFX=1 OPT_NO_RUNTIME=1",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                }
            };
            
            installProcess.Start();
            await installProcess.WaitForExitAsync();
            
            try { File.Delete(installerPath); } catch { }
            
            if (installProcess.ExitCode == 0 || installProcess.ExitCode == 3010)
            {
                StatusUpdate?.Invoke(this, "ASP.NET Core Hosting Bundle installed successfully!");
                
                // Wait for registry to update
                await Task.Delay(2000);
                
                // Restart IIS
                try
                {
                    StatusUpdate?.Invoke(this, "Restarting IIS...");
                    var iisReset = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "iisreset",
                            Arguments = "/restart",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    iisReset.Start();
                    await iisReset.WaitForExitAsync();
                    StatusUpdate?.Invoke(this, "IIS restarted successfully.");
                }
                catch (Exception iisEx)
                {
                    StatusUpdate?.Invoke(this, $"Note: Could not restart IIS ({iisEx.Message}). Please run 'iisreset' manually.");
                }
                
                await Task.Delay(1000);
                
                return true;
            }
            else
            {
                StatusUpdate?.Invoke(this, $"Installation finished with exit code {installProcess.ExitCode}.");
                return installProcess.ExitCode == 3010;
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get system information
    /// </summary>
    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        return await Task.Run(() =>
        {
            var info = new SystemInfo();
            
            try
            {
                // Get OS info
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    var os = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (os != null)
                    {
                        info.OperatingSystem = os["Caption"]?.ToString() ?? "Unknown";
                        info.Version = os["Version"]?.ToString() ?? "Unknown";
                    }
                }
                
                // Get free disk space
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == "C:\\");
                if (drive != null)
                {
                    info.FreeSpace = drive.AvailableFreeSpace;
                    info.FreeSpaceGB = (double)info.FreeSpace / (1024 * 1024 * 1024);
                }
                
                // Check if user is admin
                info.IsAdministrator = IsAdministrator();
            }
            catch
            {
                // Defaults are set
            }
            
            return info;
        });
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
}

public class SystemInfo
{
    public string OperatingSystem { get; set; } = "Unknown";
    public string Version { get; set; } = "Unknown";
    public long FreeSpace { get; set; }
    public double FreeSpaceGB { get; set; }
    public bool IsAdministrator { get; set; }
}
