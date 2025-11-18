using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace EcommerceStarter.Upgrader.Services;

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
            var totalSteps = 9; // Increased from 6 to 9 for additional cleanup
            var currentStep = 0;

            // Step 1: Stop Windows Service (if exists)
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Stopping Windows Service...",
                CurrentStep = "Service Cleanup"
            });

            await Task.Run(() => StopAndRemoveWindowsService(options.SiteName));
            await Task.Delay(500);

            // Step 2: Stop IIS Application Pool
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Stopping IIS Application Pool...",
                CurrentStep = "IIS Cleanup"
            });

            await Task.Run(() => StopIISApplicationPool(installPath));
            await Task.Delay(500);

            // Step 3: Remove IIS Website
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Removing IIS Website...",
                CurrentStep = "IIS Cleanup"
            });

            await Task.Run(() => RemoveIISWebsite(installPath));
            await Task.Delay(500);

            // Step 4: Drop Database (if requested)
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

            // Step 5: Delete Web Application Files
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Deleting web application files...",
                CurrentStep = "File Cleanup"
            });

            await Task.Run(() => DeleteFiles(installPath, options.KeepUserData));
            await Task.Delay(500);

            // Step 6: Delete Windows Service Files
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Deleting Windows Service files...",
                CurrentStep = "File Cleanup"
            });

            await Task.Run(() => DeleteWindowsServiceFiles());
            await Task.Delay(500);

            // Step 7: Delete Installer Files (self-cleanup)
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Removing installer files...",
                CurrentStep = "File Cleanup"
            });

            await Task.Run(() => DeleteInstallerFiles());
            await Task.Delay(500);

            // Step 8: Remove All Registry Entries (Programs & Features + App Config)
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Cleaning all registry entries...",
                CurrentStep = "Registry Cleanup"
            });

            await Task.Run(() => RemoveAllRegistryEntries(options.SiteName));
            await Task.Delay(500);

            // Step 9: Verify Complete Removal
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Verifying complete removal...",
                CurrentStep = "Final Verification"
            });

            var verificationResult = await VerifyCompleteRemovalAsync(options.SiteName, installPath);
            if (verificationResult.OrphanedItems.Count > 0)
            {
                result.Warnings.Add($"Found {verificationResult.OrphanedItems.Count} orphaned items: {string.Join(", ", verificationResult.OrphanedItems)}");
            }
            await Task.Delay(500);

            // Step 10: Complete
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

    private void StopAndRemoveWindowsService(string siteName)
    {
        try
        {
            var serviceName = "EcommerceStarter Background Service";

            // Check if service exists
            var checkScript = $@"
                $service = Get-Service -Name '{serviceName}' -ErrorAction SilentlyContinue;
                if ($service) {{
                    Write-Output 'EXISTS';
                }} else {{
                    Write-Output 'NOT_FOUND';
                }}
            ";

            var checkPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{checkScript}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var checkProcess = Process.Start(checkPsi))
            {
                if (checkProcess != null)
                {
                    var output = checkProcess.StandardOutput.ReadToEnd();
                    checkProcess.WaitForExit();

                    if (!output.Contains("EXISTS"))
                    {
                        // Service doesn't exist, skip
                        return;
                    }
                }
            }

            // Stop and delete service
            var removeScript = $@"
                $serviceName = '{serviceName}';
                
                # Stop service
                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue;
                Start-Sleep -Seconds 2;
                
                # Delete service
                sc.exe delete $serviceName;
                
                Write-Output 'Service removed';
            ";

            var removePsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{removeScript}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var removeProcess = Process.Start(removePsi);
            removeProcess?.WaitForExit(10000);
        }
        catch
        {
            // Non-fatal - service may not exist
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

    /// <summary>
    /// Remove ALL registry entries (Programs & Features + Application Config)
    /// </summary>
    private void RemoveAllRegistryEntries(string siteName)
    {
        try
        {
            // 1. Remove Programs & Features entry
            if (!string.IsNullOrEmpty(siteName))
            {
                var uninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(uninstallPath, false);
                
                // Also try legacy format
                var legacyUninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(legacyUninstallPath, false);
            }
            
            // Old format (backwards compatibility)
            var oldUninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter";
            Registry.LocalMachine.DeleteSubKeyTree(oldUninstallPath, false);

            // 2. Remove Application Configuration Registry (NEW)
            if (!string.IsNullOrEmpty(siteName))
            {
                var configPath = $@"SOFTWARE\EcommerceStarter\{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(configPath, false);
            }

            // 3. Remove parent key if no other sites exist
            try
            {
                using var parentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter");
                if (parentKey != null && parentKey.SubKeyCount == 0)
                {
                    // No other installations, remove parent key
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\EcommerceStarter", false);
                }
            }
            catch
            {
                // Parent key may not exist or have other installations
            }

            // 4. Remove legacy state service registry (if exists)
            _stateService.RemoveInstallationInfo();
        }
        catch
        {
            // Non-fatal - registry cleanup may fail if entries don't exist
        }
    }

    /// <summary>
    /// Delete Windows Service files from Program Files
    /// </summary>
    private void DeleteWindowsServiceFiles()
    {
        try
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var serviceDir = Path.Combine(programFilesPath, "EcommerceStarter", "WindowsService");

            if (Directory.Exists(serviceDir))
            {
                Directory.Delete(serviceDir, true);
            }
        }
        catch
        {
            // Non-fatal - files may be in use or already deleted
        }
    }

    /// <summary>
    /// Delete Installer files from Program Files (self-cleanup)
    /// </summary>
    private void DeleteInstallerFiles()
    {
        try
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var installerDir = Path.Combine(programFilesPath, "EcommerceStarter", "Installer");

            if (Directory.Exists(installerDir))
            {
                // Note: This file (UninstallService) is part of the installer
                // So we schedule deletion after current process exits
                ScheduleDeletionAfterReboot(installerDir);
            }

            // Also try to remove parent directory if empty
            var parentDir = Path.Combine(programFilesPath, "EcommerceStarter");
            if (Directory.Exists(parentDir))
            {
                try
                {
                    // Only delete if empty
                    if (!Directory.EnumerateFileSystemEntries(parentDir).Any())
                    {
                        Directory.Delete(parentDir, false);
                    }
                }
                catch
                {
                    // May have subdirectories or files in use
                }
            }
        }
        catch
        {
            // Non-fatal
        }
    }

    /// <summary>
    /// Schedule directory deletion after system reboot (for files in use)
    /// </summary>
    private void ScheduleDeletionAfterReboot(string path)
    {
        try
        {
            // Use MoveFileEx Windows API to schedule deletion on reboot
            var script = $@"
                $path = '{path.Replace("'", "''")}';
                
                # Mark all files for deletion on reboot
                Get-ChildItem -Path $path -Recurse -File | ForEach-Object {{
                    $null = cmd /c 'echo Y | del ""$($_.FullName)"" /F /Q 2>$null'
                }};
                
                # Schedule directory deletion via registry
                $regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager';
                $existing = (Get-ItemProperty -Path $regPath).PendingFileRenameOperations;
                $newValue = @($existing; ""\??\$path"", """");
                Set-ItemProperty -Path $regPath -Name 'PendingFileRenameOperations' -Value $newValue -Type MultiString;
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Non-fatal - files may remain until manual deletion
        }
    }

    /// <summary>
    /// Verify complete removal of all components
    /// </summary>
    private async Task<VerificationResult> VerifyCompleteRemovalAsync(string siteName, string installPath)
    {
        var result = new VerificationResult();

        await Task.Run(() =>
        {
            // Check Programs & Features registry
            try
            {
                var uninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{siteName}";
                using var key = Registry.LocalMachine.OpenSubKey(uninstallPath);
                if (key != null)
                {
                    result.OrphanedItems.Add("Programs & Features registry entry");
                }
            }
            catch { }

            // Check Application Config registry
            try
            {
                var configPath = $@"SOFTWARE\EcommerceStarter\{siteName}";
                using var key = Registry.LocalMachine.OpenSubKey(configPath);
                if (key != null)
                {
                    result.OrphanedItems.Add("Application configuration registry");
                }
            }
            catch { }

            // Check Windows Service
            try
            {
                var checkScript = "Get-Service -Name 'EcommerceStarter Background Service' -ErrorAction SilentlyContinue";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{checkScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (output.Trim().Length > 0)
                    {
                        result.OrphanedItems.Add("Windows Service still installed");
                    }
                }
            }
            catch { }

            // Check IIS Application
            try
            {
                var siteName = Path.GetFileName(installPath.TrimEnd('\\'));
                var checkScript = $"Get-WebApplication -Name '{siteName}' -Site 'Default Web Site' -ErrorAction SilentlyContinue";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Import-Module WebAdministration; {checkScript}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (output.Trim().Length > 0)
                    {
                        result.OrphanedItems.Add("IIS Application still exists");
                    }
                }
            }
            catch { }

            // Check web application files
            if (Directory.Exists(installPath))
            {
                result.OrphanedItems.Add("Web application files still exist");
            }

            // Check Windows Service files
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var serviceDir = Path.Combine(programFilesPath, "EcommerceStarter", "WindowsService");
            if (Directory.Exists(serviceDir))
            {
                result.OrphanedItems.Add("Windows Service files still exist");
            }
        });

        return result;
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

public class VerificationResult
{
    public List<string> OrphanedItems { get; set; } = new();
}
