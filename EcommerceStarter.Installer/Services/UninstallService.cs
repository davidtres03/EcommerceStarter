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
            var installInfo = !string.IsNullOrWhiteSpace(options.SiteName)
                ? _stateService.GetInstallationInfo(options.SiteName)
                : _stateService.GetInstallationInfo();
            if (installInfo == null)
            {
                return new UninstallResult
                {
                    Success = false,
                    ErrorMessage = "No installation found. EcommerceStarter may already be uninstalled."
                };
            }

            var installPath = installInfo.InstallPath;
            var totalSteps = 8; // Instance uninstall steps (no installer removal)
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
            await Task.Delay(2000); // Wait 2 seconds for connections to close

            // Step 3: Remove IIS Website
            progress?.Report(new UninstallProgress
            {
                Percentage = ++currentStep * 100 / totalSteps,
                Message = "Removing IIS Website...",
                CurrentStep = "IIS Cleanup"
            });

            await Task.Run(() => RemoveIISWebsite(installPath));
            await Task.Delay(1000); // Wait 1 second after site removal

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

            await Task.Run(() => DeleteWindowsServiceFiles(installPath));
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

            // Step 11: Complete
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

    /// <summary>
    /// Uninstall the EcommerceStarter installer completely.
    /// This removes the installer from Program Files and the Uninstall registry.
    /// This is ONLY called when uninstalling the installer itself, NOT when removing instances.
    /// BLOCKED if any instances exist - user must remove all instances first.
    /// </summary>
    public async Task<UninstallResult> UninstallInstallerAsync()
    {
        try
        {
            // Check if any instances exist - BLOCK uninstall if they do
            using var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter");
            var instanceKeys = baseKey?.GetSubKeyNames() ?? Array.Empty<string>();

            if (instanceKeys.Length > 0)
            {
                // Build list of instance names
                var instanceNames = string.Join(", ", instanceKeys);

                return new UninstallResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot uninstall EcommerceStarter installer while {instanceKeys.Length} instance(s) are installed: {instanceNames}\n\n" +
                                   $"Please use the 'Change' button to remove all instances first, then uninstall the installer."
                };
            }

            // No instances - safe to remove the installer
            await Task.Run(() => RemoveInstallerCompletely());

            return new UninstallResult
            {
                Success = true,
                Message = "EcommerceStarter installer removed successfully."
            };
        }
        catch (Exception ex)
        {
            return new UninstallResult
            {
                Success = false,
                ErrorMessage = $"Failed to uninstall installer: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Remove the EcommerceStarter installer from Program Files and Uninstall registry.
    /// This is ONLY called when uninstalling the installer itself, NOT when removing instances.
    /// Instances are independent - the installer can exist with 0 instances.
    /// </summary>
    private void RemoveInstallerCompletely()
    {
        try
        {
            // Remove global Uninstall registry entry
            try
            {
                using var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", writable: true);
                uninstallKey?.DeleteSubKeyTree("EcommerceStarter", throwOnMissingSubKey: false);
            }
            catch
            {
                // Non-fatal
            }

            // Remove Program Files\EcommerceStarter directory
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var rootDir = Path.Combine(programFilesPath, "EcommerceStarter");
            if (!Directory.Exists(rootDir)) return;

            try
            {
                Directory.Delete(rootDir, true);
            }
            catch
            {
                // If files are locked, schedule deletion on reboot
                ScheduleDeletionAfterReboot(rootDir);
            }
        }
        catch
        {
            // Non-fatal
        }
    }

    private void StopAndRemoveWindowsService(string siteName)
    {
        try
        {
            var serviceName = !string.IsNullOrWhiteSpace(siteName)
                ? $"EcommerceStarter-{siteName}"
                : "EcommerceStarter Background Service"; // legacy fallback

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

            // Newer installer creates a root Website named {siteName}.
            // Try to remove a root Website and its AppPool; then fall back to removing an Application under Default Web Site.
            var script = $@"
                Import-Module WebAdministration;
                # Try remove root Website first
                if (Get-WebSite -Name '{siteName}' -ErrorAction SilentlyContinue) {{
                    Remove-WebSite -Name '{siteName}' -ErrorAction SilentlyContinue;
                }}
                # Remove matching App Pool if exists
                if (Test-Path IIS:\AppPools\{siteName}) {{
                    Remove-WebAppPool -Name '{siteName}' -ErrorAction SilentlyContinue;
                }}
                # Fallback: remove as Application under Default Web Site (legacy installs)
                Remove-WebApplication -Name '{siteName}' -Site 'Default Web Site' -ErrorAction SilentlyContinue;
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(15000);
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

            // First, check if database exists
            var checkCmd = new SqlCommand("SELECT database_id FROM sys.databases WHERE name = @dbName", connection);
            checkCmd.Parameters.AddWithValue("@dbName", databaseName);
            var exists = await checkCmd.ExecuteScalarAsync();

            if (exists == null)
            {
                return (true, "Database does not exist (already removed)");
            }

            // Kill all active connections to the database
            var killConnectionsCmd = new SqlCommand(
                $"DECLARE @kill varchar(8000) = ''; " +
                $"SELECT @kill = @kill + 'KILL ' + CONVERT(varchar(5), session_id) + ';' " +
                $"FROM sys.dm_exec_sessions " +
                $"WHERE database_id = DB_ID(@dbName) AND session_id <> @@SPID; " +
                $"EXEC(@kill);",
                connection);
            killConnectionsCmd.Parameters.AddWithValue("@dbName", databaseName);
            killConnectionsCmd.CommandTimeout = 60;

            try
            {
                await killConnectionsCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Non-fatal if no connections to kill
            }

            // Wait a moment for connections to fully terminate
            await Task.Delay(500);

            // Set database to single user mode and drop
            var dropCmd = new SqlCommand(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{databaseName}];",
                connection);
            dropCmd.CommandTimeout = 60;

            await dropCmd.ExecuteNonQueryAsync();

            return (true, "Database dropped successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to drop database: {ex.Message}");
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
            // Remove Programs & Features entry for this instance only
            if (!string.IsNullOrEmpty(siteName))
            {
                var uninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(uninstallPath, false);

                // Also try alternative naming (legacy or custom)
                var altUninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(altUninstallPath, false);
            }

            // Remove per-instance application configuration registry ONLY for this site
            if (!string.IsNullOrEmpty(siteName))
            {
                var configPath = $@"SOFTWARE\EcommerceStarter\{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(configPath, false);
            }

            // DO NOT remove parent key (SOFTWARE\EcommerceStarter) to preserve other instances
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
            // 1. Remove Programs & Features entry for this instance
            if (!string.IsNullOrEmpty(siteName))
            {
                var uninstallInstancePath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(uninstallInstancePath, false);

                // Also try alternate/legacy naming
                var altUninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(altUninstallPath, false);

                var legacyUninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter";
                // Do NOT remove the plain 'EcommerceStarter' key unless it's this exact instance (avoid nuking others)
                // Skip deleting legacy global key to be safe in multi-instance environments.
            }

            // 2. Remove per-instance Application Configuration registry
            if (!string.IsNullOrEmpty(siteName))
            {
                var configPath = $@"SOFTWARE\EcommerceStarter\{siteName}";
                Registry.LocalMachine.DeleteSubKeyTree(configPath, false);
            }

            // 3. Never delete the parent key (SOFTWARE\EcommerceStarter); preserve other instances
            // 4. Do not call legacy state removal that deletes the entire tree
        }
        catch
        {
            // Non-fatal - registry cleanup may fail if entries don't exist
        }
    }

    /// <summary>
    /// Delete Windows Service files from Program Files
    /// </summary>
    private void DeleteWindowsServiceFiles(string installPath)
    {
        try
        {
            // Instance-specific service files live under the installation path
            var serviceDir = Path.Combine(installPath ?? string.Empty, "service");

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
                var uninstallPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";
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
                var svcName = !string.IsNullOrWhiteSpace(siteName) ? $"EcommerceStarter-{siteName}" : "EcommerceStarter Background Service";
                var checkScript = $"Get-Service -Name '{svcName}' -ErrorAction SilentlyContinue";
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
                var checkScript = $@"
                    Import-Module WebAdministration;
                    $existsRoot = Get-WebSite -Name '{siteName}' -ErrorAction SilentlyContinue;
                    if ($existsRoot) {{ Write-Output 'ROOT_WEBSITE_EXISTS' }}
                    $existsApp = Get-WebApplication -Name '{siteName}' -Site 'Default Web Site' -ErrorAction SilentlyContinue;
                    if ($existsApp) {{ Write-Output 'APP_EXISTS' }}
                ";
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
                    var trimmed = output.Trim();
                    if (trimmed.Contains("ROOT_WEBSITE_EXISTS")) result.OrphanedItems.Add("IIS Website still exists");
                    if (trimmed.Contains("APP_EXISTS")) result.OrphanedItems.Add("IIS Application still exists");
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
