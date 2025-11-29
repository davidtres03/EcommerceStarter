using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;
using EcommerceStarter.Installer;
using EcommerceStarter.Installer.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// RepairPage - Shows repair/verification status
/// </summary>
public partial class RepairPage : Page
{
    private ExistingInstallation? _install;

    public RepairPage() : this(null) { }

    public RepairPage(ExistingInstallation? install)
    {
        InitializeComponent();
        _install = install;
        Loaded += RepairPage_Loaded;
    }

    private async void RepairPage_Loaded(object sender, RoutedEventArgs e)
    {
        await PerformRepairAsync();
    }

    private async Task PerformRepairAsync()
    {
        try
        {
            Append("Starting repair...");

            if (_install == null)
            {
                Append("No installation context provided. Attempting auto-detect...");
                var detection = new UpgradeDetectionService();
                var all = await detection.DetectAllInstallationsAsync();
                _install = all.FirstOrDefault();
                if (_install == null)
                {
                    Append("No installations found. Nothing to repair.");
                    return;
                }
            }

            Append($"Site: {_install.SiteName}");
            Append($"Path: {_install.InstallPath}");

            // 1) Verify and restore application files
            var restored = await VerifyAndRestoreFilesAsync(_install.InstallPath);
            Append(restored);

            // 2) Repair IIS configuration
            var iisResult = await RepairIisAsync(_install.SiteName, _install.InstallPath);
            Append(iisResult);

            // 3) Verify and restore Windows service
            var serviceResult = await VerifyAndRestoreServiceAsync(_install);
            Append(serviceResult);

            // 4) Regenerate web.config with correct APP_POOL_ID
            var webConfigResult = await RegenerateWebConfigAsync(_install);
            Append(webConfigResult);

            // 5) Validate database connectivity
            var validator = new ConfigurationValidationService();
            var dbResult = await validator.ValidateDatabaseConnectionAsync(_install.DatabaseServer, _install.DatabaseName, createIfNotExists: false);
            Append($"Database check: {(dbResult.IsSuccess ? "OK" : "FAILED")} - {dbResult.Message}");

            Append("Repair completed.");

            MessageBox.Show("Repair completed. See details on this page.", "Repair Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh detected instances so the Maintenance view reflects the new state
            if (Application.Current?.MainWindow is MainWindow mw)
            {
                mw.ShowInstanceSelection();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error during repair:\n\n{ex.Message}",
                "Repair Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Append(string text)
    {
        if (RepairOutput != null)
        {
            RepairOutput.Text += text + Environment.NewLine;
        }
    }

    private async Task<string> VerifyAndRestoreFilesAsync(string installPath)
    {
        try
        {
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            var missing = new System.Collections.Generic.List<string>();
            string[] critical =
            {
                "EcommerceStarter.dll",
                "EcommerceStarter.deps.json",
                "EcommerceStarter.runtimeconfig.json",
                "wwwroot"
            };

            foreach (var item in critical)
            {
                var path = Path.Combine(installPath, item);
                if (!File.Exists(path) && !Directory.Exists(path))
                    missing.Add(item);
            }

            // Note: appsettings.json is NOT used - all configuration in Windows Registry
            // Connection string stored encrypted at: HKLM:\SOFTWARE\EcommerceStarter\{SiteName}\ConnectionStringEncrypted
            // No file-based configuration needed for repair

            if (missing.Count == 0)
            {
                return "Files: OK";
            }

            Append($"Missing items: {string.Join(", ", missing)}. Restoring...");

            // Locate bundled app files in package: either ../Application or ./app
            var installerDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parent = Directory.GetParent(installerDir)?.FullName;
            var dirName = Path.GetFileName(installerDir);

            string? bundle = null;
            if (parent != null && dirName.Equals("Installer", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = Path.Combine(parent, "Application");
                if (Directory.Exists(candidate)) bundle = candidate;
            }
            if (bundle == null)
            {
                var local = Path.Combine(installerDir, "app");
                if (Directory.Exists(local)) bundle = local;
            }

            if (bundle == null)
            {
                return "Files: Missing items detected but no bundled application found to restore from.";
            }

            await Task.Run(() => CopyIfMissing(bundle, installPath));
            return "Files: Restored missing items.";
        }
        catch (Exception ex)
        {
            return $"Files: Error - {ex.Message}";
        }
    }

    private void CopyIfMissing(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            var target = Path.Combine(dest, name);
            if (!File.Exists(target))
            {
                File.Copy(file, target, overwrite: false);
            }
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(dir);
            var target = Path.Combine(dest, name);
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }
            CopyIfMissing(dir, target);
        }
    }

    private async Task<string> RepairIisAsync(string siteName, string physicalPath)
    {
        try
        {
            var script = $@"
Import-Module WebAdministration;

$siteName = '{siteName}';
$appPool = '{siteName}';
$path = '{physicalPath}';

if (-not (Test-Path IIS:\\AppPools\\$appPool)) {{
  New-WebAppPool -Name $appPool | Out-Null;
  Set-ItemProperty IIS:\\AppPools\\$appPool -Name managedRuntimeVersion -Value '';
  Set-ItemProperty IIS:\\AppPools\\$appPool -Name enable32BitAppOnWin64 -Value $false;
}}

# Create site if missing; otherwise ensure physical path
$site = Get-WebSite -Name $siteName -ErrorAction SilentlyContinue;
if ($site) {{
  Set-ItemProperty IIS:\\Sites\\$siteName -Name physicalPath -Value $path;
  Set-ItemProperty IIS:\\Sites\\$siteName -Name applicationPool -Value $appPool;
}} else {{
  $port = 8080;
  while (Get-WebBinding -Port $port -ErrorAction SilentlyContinue) {{ $port++; }}
  New-WebSite -Name $siteName -PhysicalPath $path -ApplicationPool $appPool -Port $port | Out-Null;
}}

Start-WebAppPool -Name $appPool -ErrorAction SilentlyContinue;
";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0) return "IIS: OK";
                var err = await proc.StandardError.ReadToEndAsync();
                return $"IIS: Error - {err}";
            }
            return "IIS: Error - could not start PowerShell";
        }
        catch (Exception ex)
        {
            return $"IIS: Error - {ex.Message}";
        }
    }

    private async Task<string> RegenerateWebConfigAsync(ExistingInstallation install)
    {
        try
        {
            // Build InstallationConfig from existing installation
            var config = new InstallationConfig
            {
                SiteName = install.SiteName,
                CompanyName = install.CompanyName,
                InstallationPath = install.InstallPath,
                DatabaseServer = install.DatabaseServer,
                DatabaseName = install.DatabaseName,
                Port = install.LocalhostPort  // Use LocalhostPort property
            };

            var installationService = new InstallationService();
            await installationService.ApplyConfigurationAsync(config);
            return "web.config: Regenerated with APP_POOL_ID";
        }
        catch (Exception ex)
        {
            return $"web.config: Error - {ex.Message}";
        }
    }

    private async Task<string> VerifyAndRestoreServiceAsync(ExistingInstallation install)
    {
        try
        {
            var serviceName = $"EcommerceStarter-{install.SiteName}";
            var servicePath = Path.Combine(install.InstallPath, "service");
            var serviceExe = Path.Combine(servicePath, "EcommerceStarter.WindowsService.exe");

            // Check if service directory exists and has required files
            var serviceFilesOk = Directory.Exists(servicePath) && File.Exists(serviceExe);
            
            if (!serviceFilesOk)
            {
                Append($"Service files missing. Restoring to {servicePath}...");
                
                // Locate bundled service files
                var installerDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var parent = Directory.GetParent(installerDir)?.FullName;
                var dirName = Path.GetFileName(installerDir);

                string? serviceBundle = null;
                if (parent != null && dirName.Equals("Installer", StringComparison.OrdinalIgnoreCase))
                {
                    var candidate = Path.Combine(parent, "WindowsService");
                    if (Directory.Exists(candidate)) serviceBundle = candidate;
                }
                if (serviceBundle == null)
                {
                    var local = Path.Combine(installerDir, "service");
                    if (Directory.Exists(local)) serviceBundle = local;
                }

                if (serviceBundle == null)
                {
                    return "Service: Missing files but no bundled service found to restore from.";
                }

                Directory.CreateDirectory(servicePath);
                await Task.Run(() => CopyDirectory(serviceBundle, servicePath));
                Append("Service files restored.");
            }

            // Check if Windows service is installed
            var serviceInstalled = await IsServiceInstalledAsync(serviceName);
            
            if (!serviceInstalled)
            {
                Append($"Windows service not installed. Installing {serviceName}...");
                var installResult = await InstallWindowsServiceAsync(serviceName, serviceExe);
                if (!installResult.Success)
                {
                    return $"Service: Installation failed - {installResult.Message}";
                }
                Append("Windows service installed.");
            }
            else
            {
                // Service exists, verify it's pointing to correct path
                var updateResult = await UpdateServicePathAsync(serviceName, serviceExe);
                if (!updateResult.Success && updateResult.Message != "ALREADY_CORRECT")
                {
                    return $"Service: Path update failed - {updateResult.Message}";
                }
            }

            // Ensure service is running
            var startResult = await StartServiceAsync(serviceName);
            if (!startResult.Success)
            {
                return $"Service: Start failed - {startResult.Message}";
            }

            return "Service: OK (files verified, service installed and running)";
        }
        catch (Exception ex)
        {
            return $"Service: Error - {ex.Message}";
        }
    }

    private void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            var target = Path.Combine(dest, name);
            File.Copy(file, target, overwrite: true);
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(dir);
            var target = Path.Combine(dest, name);
            CopyDirectory(dir, target);
        }
    }

    private async Task<bool> IsServiceInstalledAsync(string serviceName)
    {
        var script = $@"
$service = Get-Service -Name '{serviceName}' -ErrorAction SilentlyContinue;
if ($service) {{ Write-Output 'EXISTS'; }} else {{ Write-Output 'NOT_FOUND'; }}
";
        var result = await RunPowerShellAsync(script);
        return result.Output?.Trim() == "EXISTS";
    }

    private async Task<(bool Success, string Message)> InstallWindowsServiceAsync(string serviceName, string exePath)
    {
        var script = $@"
sc.exe create '{serviceName}' binPath='{exePath}' start=auto DisplayName='EcommerceStarter Background Service ({Path.GetFileNameWithoutExtension(serviceName).Replace("EcommerceStarter-", "")})';
if ($LASTEXITCODE -eq 0) {{ Write-Output 'SUCCESS'; }} else {{ Write-Output 'FAILED'; }}
";
        var result = await RunPowerShellAsync(script);
        return result.Output?.Contains("SUCCESS") == true 
            ? (true, "Service installed") 
            : (false, result.Error ?? "Unknown error");
    }

    private async Task<(bool Success, string Message)> UpdateServicePathAsync(string serviceName, string exePath)
    {
        var script = $@"
$service = Get-WmiObject -Class Win32_Service -Filter ""Name='$serviceName'"";
if ($service.PathName -ne '{exePath}') {{
    sc.exe config '{serviceName}' binPath='{exePath}';
    Write-Output 'UPDATED';
}} else {{
    Write-Output 'ALREADY_CORRECT';
}}
";
        var result = await RunPowerShellAsync(script);
        return result.Output?.Contains("UPDATED") == true 
            ? (true, "Service path updated") 
            : (true, result.Output ?? "Already correct");
    }

    private async Task<(bool Success, string Message)> StartServiceAsync(string serviceName)
    {
        var script = $@"
Start-Service -Name '{serviceName}' -ErrorAction SilentlyContinue;
$service = Get-Service -Name '{serviceName}' -ErrorAction SilentlyContinue;
if ($service -and $service.Status -eq 'Running') {{ Write-Output 'RUNNING'; }} else {{ Write-Output 'FAILED'; }}
";
        var result = await RunPowerShellAsync(script);
        return result.Output?.Contains("RUNNING") == true 
            ? (true, "Service running") 
            : (false, "Service failed to start");
    }

    private async Task<(string? Output, string? Error)> RunPowerShellAsync(string script)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "`\"")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc == null) return (null, "Could not start PowerShell");

        var output = await proc.StandardOutput.ReadToEndAsync();
        var error = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        return (output, string.IsNullOrWhiteSpace(error) ? null : error);
    }
}


