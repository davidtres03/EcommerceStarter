using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for uninstalling EcommerceStarter
/// </summary>
public class UninstallService
{
    private readonly InstallationStateService _stateService;

    public UninstallService()
    {
        _stateService = new InstallationStateService();
    }

    /// <summary>
    /// Perform complete uninstallation
    /// </summary>
    public async Task<UninstallResult> UninstallAsync(
        UninstallOptions options,
        IProgress<UninstallProgress> progress)
    {
        // ?? DEMO MODE PROTECTION - Refuse to run!
        if (App.IsDemoMode)
        {
            // Simulate uninstall progress for demo purposes only
            await SimulateDemoUninstallAsync(options, progress);
            
            return new UninstallResult
            {
                Success = true,
                Message = "Demo uninstall completed successfully (no real changes were made)"
            };
        }
        
        var result = new UninstallResult { Success = true };

        try
        {
            // Get installation info
            var installInfo = _stateService.GetInstallationInfo();
            if (installInfo == null)
            {
                return new UninstallResult
                {
                    Success = false,
                    ErrorMessage = "No installation found. EcommerceStarter may already be uninstalled."
                };
            }

            var installPath = installInfo.InstallPath;
            var totalSteps = 6;
            var currentStep = 0;

            // Step 1: Stop IIS Application Pool
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Stopping IIS Application Pool...",
                CurrentStep = "IIS Cleanup"
            });

            await Task.Run(() => StopIISApplicationPool(installPath));
            await Task.Delay(500);

            // Step 2: Remove IIS Website
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Removing IIS Website...",
                CurrentStep = "IIS Cleanup"
            });

            await Task.Run(() => RemoveIISWebsite(installPath));
            await Task.Delay(500);

            // Step 3: Drop Database (if requested)
            if (options.RemoveDatabase)
            {
                progress?.Report(new UninstallProgress
                {
                    Percentage = ++currentStep * 100 / totalSteps,
                    Message = "Dropping database...",
                    CurrentStep = "Database Cleanup"
                });

                var dbResult = await DropDatabaseAsync(options.DatabaseServer, options.DatabaseName);
                if (!dbResult.Success)
                {
                    result.Warnings.Add($"Database removal warning: {dbResult.Message}");
                }
                await Task.Delay(500);
            }
            else
            {
                progress?.Report(new UninstallProgress
                {
                    Percentage = ++currentStep * 100 / totalSteps,
                    Message = "Skipping database removal (keeping data)...",
                    CurrentStep = "Database Cleanup"
                });
                await Task.Delay(300);
            }

            // Step 4: Delete Files
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Deleting application files...",
                CurrentStep = "File Cleanup"
            });

            await Task.Run(() => DeleteFiles(installPath, options.KeepUserData));
            await Task.Delay(500);

            // Step 5: Remove Registry Entries
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Cleaning registry entries...",
                CurrentStep = "Registry Cleanup"
            });

            await Task.Run(() => RemoveRegistryEntries(options.SiteName));
            await Task.Delay(500);

            // Step 6: Complete
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Finalizing cleanup...",
                CurrentStep = "Final Cleanup"
            });

            await Task.Delay(500);

            // Complete
            progress?.Report(new UninstallProgress
            {
                Percentage = 100,
                Message = "Uninstallation complete!",
                CurrentStep = "Complete"
            });

            result.Success = true;
            result.Message = "EcommerceStarter has been successfully uninstalled.";
            return result;
        }
        catch (Exception ex)
        {
            return new UninstallResult
            {
                Success = false,
                ErrorMessage = $"Uninstallation failed: {ex.Message}"
            };
        }
    }

    private void StopIISApplicationPool(string installPath)
    {
        try
        {
            // Try to stop the app pool
            var siteName = Path.GetFileName(installPath.TrimEnd('\\'));
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Stop-WebAppPool -Name '{siteName}' -ErrorAction SilentlyContinue\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(10000); // 10 second timeout
        }
        catch
        {
            // Non-fatal - continue with uninstall
        }
    }

    private void RemoveIISWebsite(string installPath)
    {
        try
        {
            var siteName = Path.GetFileName(installPath.TrimEnd('\\'));
            
            // The installer creates an APPLICATION under Default Web Site, not a standalone website
            // So we need to remove the web application, not a website
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Import-Module WebAdministration; Remove-WebApplication -Name '{siteName}' -Site 'Default Web Site' -ErrorAction SilentlyContinue; Remove-WebAppPool -Name '{siteName}' -ErrorAction SilentlyContinue\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                Verb = "runas" // Need admin to remove IIS config
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(10000);
        }
        catch
        {
            // Non-fatal
        }
    }

    private async Task<(bool Success, string Message)> DropDatabaseAsync(string server, string databaseName)
    {
        try
        {
            var connectionString = $"Server={server};Database=master;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Set database to single user mode to drop
            var setSingleUserCmd = new SqlCommand(
                $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @dbName) " +
                $"BEGIN " +
                $"  ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"  DROP DATABASE [{databaseName}]; " +
                $"END",
                connection);
            setSingleUserCmd.Parameters.AddWithValue("@dbName", databaseName);

            await setSingleUserCmd.ExecuteNonQueryAsync();

            return (true, "Database dropped successfully");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void DeleteFiles(string installPath, bool keepUserData)
    {
        try
        {
            if (!Directory.Exists(installPath))
                return;

            if (keepUserData)
            {
                // Delete application files but keep user data (uploads, logs, etc.)
                var protectedFolders = new[] { "wwwroot\\uploads", "logs", "App_Data" };

                foreach (var file in Directory.GetFiles(installPath, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(installPath, file);
                    var isProtected = protectedFolders.Any(folder => relativePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase));

                    if (!isProtected)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Continue if file is locked
                        }
                    }
                }
            }
            else
            {
                // Delete everything
                Directory.Delete(installPath, true);
            }
        }
        catch
        {
            // Non-fatal - some files may be in use
        }
    }

    private void RemoveRegistryEntries(string siteName)
    {
        try
        {
            // Remove custom tracking registry (SOFTWARE\EcommerceStarter)
            _stateService.RemoveInstallationInfo();

            // Remove Programs & Features entry (created by InstallationService with site name)
            if (!string.IsNullOrEmpty(siteName))
            {
                var uninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(uninstallPath, false);
            }
            
            // Also try to remove old entry without site name (for backwards compatibility)
            var oldUninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter";
            Registry.LocalMachine.DeleteSubKeyTree(oldUninstallPath, false);
        }
        catch
        {
            // Non-fatal - registry cleanup may fail if entries don't exist
        }
    }

    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    // REMOVED OLD VERSION - REPLACED WITH DETAILED VERSION BELOW
    /// </summary>
    private async Task SimulateDemoUninstallAsync(
        UninstallOptions options,
        IProgress<UninstallProgress> progress)
    {
        var steps = new[]
        {
            "Stopping IIS Application Pool... (simulated)",
            "Removing IIS Website... (simulated)",
            "Deleting application files... (simulated)",
            options.RemoveDatabase ? "Removing database... (simulated)" : "Skipping database removal",
            "Cleaning up registry... (simulated)",
            "Finalizing uninstallation... (simulated)"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            progress?.Report(new UninstallProgress
            {
                Percentage = (i + 1) * 100 / steps.Length,
                Message = $"?? DEMO: {steps[i]}",
                CurrentStep = $"Step {i + 1} of {steps.Length}"
            });

            // Simulate work with delay
            await Task.Delay(800);
        }

        progress?.Report(new UninstallProgress
        {
            Percentage = 100,
            Message = "?? Demo uninstallation complete - No real changes were made!",
            CurrentStep = "Complete"
        });
    }
}

public class UninstallOptions
{
    public bool RemoveDatabase { get; set; } = false;
    public bool KeepUserData { get; set; } = false;
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
}

public class UninstallProgress
{
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
}

public class UninstallResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}
