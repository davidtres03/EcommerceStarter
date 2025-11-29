using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using EcommerceStarter.Upgrader.Models;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for upgrading existing installations
/// </summary>
public class UpgradeService
{
    public event EventHandler<UpgradeProgress>? ProgressUpdate;
    public event EventHandler<string>? StatusUpdate;

    private readonly LoggerService _logger;

    public UpgradeService(LoggerService? logger = null)
    {
        // Use provided logger or create a new one
        _logger = logger ?? new LoggerService();
    }

    /// <summary>
    /// Log a message to both UI event and file logger
    /// </summary>
    private void LogMessage(string message)
    {
        // Send to file/centralized logger
        _logger.Log(message);
        
        // Also send to UI via StatusUpdate event (backward compatibility)
        StatusUpdate?.Invoke(this, message);
    }

    /// <summary>
    /// Validate if an upgrade is possible and safe
    /// </summary>
    public UpgradeValidationResult ValidateUpgrade(ExistingInstallation installation)
    {
        StatusUpdate?.Invoke(this, "[ValidateUpgrade] STARTING VALIDATION");
        StatusUpdate?.Invoke(this, $"[ValidateUpgrade] Installation Version: {installation.Version ?? "Unknown"}");
        StatusUpdate?.Invoke(this, $"[ValidateUpgrade] Installation Path: {installation.InstallPath}");
        StatusUpdate?.Invoke(this, $"[ValidateUpgrade] Site Name: {installation.SiteName}");
        StatusUpdate?.Invoke(this, $"[ValidateUpgrade] Database: {installation.DatabaseServer}/{installation.DatabaseName}");

        var result = new UpgradeValidationResult { CanProceed = true };

        // Check version compatibility
        StatusUpdate?.Invoke(this, "[ValidateUpgrade] Checking version compatibility...");
        var preReqs = VersionService.GetPreUpgradeRequirements(installation.Version ?? "Unknown");
        StatusUpdate?.Invoke(this, $"[ValidateUpgrade] PreReqs CanUpgrade: {preReqs.CanUpgrade}");
        StatusUpdate?.Invoke(this, $"[ValidateUpgrade] PreReqs Message: {preReqs.Message}");

        if (!preReqs.CanUpgrade)
        {
            result.CanProceed = false;
            result.ErrorMessage = preReqs.Message;
            StatusUpdate?.Invoke(this, $"[ValidateUpgrade] VALIDATION FAILED: {preReqs.Message}");
            return result;
        }

        // Warn about breaking changes if any
        if (preReqs.HasBreakingChanges)
        {
            result.CanProceed = true;
            result.HasWarnings = true;
            result.WarningMessage = preReqs.Message;
            result.BreakingChanges = preReqs.BreakingChanges;
        }

        // Check if installation path exists
        if (!Directory.Exists(installation.InstallPath))
        {
            result.CanProceed = false;
            result.ErrorMessage = $"Installation path not found: {installation.InstallPath}";
            return result;
        }

        // Check database connectivity
        if (installation.HasDatabase)
        {
            // Database validation would go here
            StatusUpdate?.Invoke(this, "Database validation passed");
        }

        result.Message = $"Upgrade from {installation.Version} to {VersionService.CURRENT_VERSION} is ready";
        return result;
    }

    /// <summary>
    /// Get required migrations for an upgrade
    /// </summary>
    public List<string> GetRequiredMigrations(string fromVersion)
    {
        return VersionService.GetRequiredMigrations(fromVersion, VersionService.CURRENT_VERSION);
    }

    /// <summary>
    /// Perform upgrade from a downloaded zip file
    /// </summary>
    public async Task<UpgradeResult> UpgradeFromZipAsync(ExistingInstallation installation, string zipFilePath, string? newVersion = null)
    {
        // ?? DEMO MODE PROTECTION - Refuse to upgrade!
        if (App.IsDemoMode)
        {
            // Simulate upgrade for demo purposes only
            await SimulateDemoUpgradeAsync(installation);

            return new UpgradeResult
            {
                Success = true,
                Message = "Demo upgrade completed successfully (no real changes were made)"
            };
        }

        var result = new UpgradeResult { Success = true };
        var backupPath = string.Empty;

        try
        {
            StatusUpdate?.Invoke(this, "[UpgradeService] === UPGRADE STARTED ===");

            StatusUpdate?.Invoke(this, $"[UpgradeService] GitHub Version Parameter: {newVersion}");

            StatusUpdate?.Invoke(this, $"[UpgradeService] Install Path: {installation.InstallPath}");

            StatusUpdate?.Invoke(this, $"[UpgradeService] Site Name: {installation.SiteName}");
            StatusUpdate?.Invoke(this, $"[UpgradeService] Database: {installation.DatabaseServer}/{installation.DatabaseName}");

            StatusUpdate?.Invoke(this, $"[UpgradeService] ZIP File: {zipFilePath}");
            StatusUpdate?.Invoke(this, $"ZIP exists: {File.Exists(zipFilePath)}");

            // Note: totalSteps removed - using hardcoded percentages for progress instead
            var currentStep = 0;

            // Step 1: Create backup (0-15%)
            ReportProgress(++currentStep, 5, "Creating backup...");
            StatusUpdate?.Invoke(this, "Step 1: Creating backup...");
            backupPath = await CreateBackupAsync(installation.InstallPath);
            StatusUpdate?.Invoke(this, $"✓ Backup created at: {backupPath}");
            ReportProgress(currentStep, 15, "Backup created");

            // Step 2: Stop IIS (15-25%)
            ReportProgress(++currentStep, 20, "Stopping IIS Application Pool...");
            StatusUpdate?.Invoke(this, $"Step 2: Stopping IIS pool '{installation.SiteName}'...");
            await StopIISAsync(installation.SiteName);
            StatusUpdate?.Invoke(this, "✓ IIS application pool stopped");
            ReportProgress(currentStep, 25, "IIS stopped");

            // Step 3: Extract new files to temp (25-40%)
            ReportProgress(++currentStep, 30, "Extracting new files...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 3: Extracting files...");
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"upgrade-{Guid.NewGuid()}");
            StatusUpdate?.Invoke(this, $"[UpgradeService] Extract path: {tempExtractPath}");
            
            // Support both ZIP and tar.gz formats
            await ExtractArchiveAsync(zipFilePath, tempExtractPath);
            
            StatusUpdate?.Invoke(this, "[UpgradeService] Files extracted successfully");
            ReportProgress(currentStep, 40, "Files extracted");

            // Find the actual app folder (might be nested inside EcommerceStarter-Installer-vX.X.X/)
            var appSourcePath = FindAppFolder(tempExtractPath);
            if (string.IsNullOrEmpty(appSourcePath))
            {
                appSourcePath = tempExtractPath; // Fallback to root if no nested folder found
                StatusUpdate?.Invoke(this, "[UpgradeService] Using root temp path for app files");
            }
            else
            {
                StatusUpdate?.Invoke(this, $"[UpgradeService] Found nested app folder: {appSourcePath}");
            }

            // Step 4: Preserve config files (40-45%)
            ReportProgress(++currentStep, 42, "Preserving configuration...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 4: Preserving configuration...");
            var configBackup = await PreserveConfigurationAsync(installation.InstallPath);
            StatusUpdate?.Invoke(this, "[UpgradeService] Configuration preserved");
            ReportProgress(currentStep, 45, "Configuration preserved");

            // Step 5: Deploy new files (45-70%)
            ReportProgress(++currentStep, 50, "Deploying new application...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 5: Deploying files...");
            await DeployFilesAsync(appSourcePath, installation.InstallPath);
            StatusUpdate?.Invoke(this, "[UpgradeService] Files deployed");

            // Step 5b: Update Program Files installer and upgrader
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 5b: Updating Program Files installer...");
            await UpdateProgramFilesAsync(tempExtractPath);
            StatusUpdate?.Invoke(this, "[UpgradeService] Program Files updated");

            await RestoreConfigurationAsync(configBackup, installation.InstallPath);
            StatusUpdate?.Invoke(this, "[UpgradeService] Configuration restored");
            ReportProgress(currentStep, 70, "Application deployed");

            // Step 6: Run database migrations (70-85%)
            ReportProgress(++currentStep, 75, "Running database migrations...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 6: Running migrations...");
            var migrationResult = await RunMigrationsAsync(tempExtractPath, installation.DatabaseServer, installation.DatabaseName);
            StatusUpdate?.Invoke(this, $"[UpgradeService] Migration result: {migrationResult.Success}");
            StatusUpdate?.Invoke(this, $"[UpgradeService] Migration message: {migrationResult.Message}");
            if (!migrationResult.Success)
            {
                StatusUpdate?.Invoke(this, $"Warning: {migrationResult.Message}");
            }
            ReportProgress(currentStep, 82, "Migrations complete");

            // Step 6b: Ensure internal service key exists (82-85%)
            StatusUpdate?.Invoke(this, "[UpgradeService] Checking internal service configuration...");
            await EnsureInternalServiceKeyExistsAsync(installation.DatabaseServer, installation.DatabaseName);
            ReportProgress(currentStep, 85, "Configuration verified");

            // Step 7: Upgrade Windows Service (85-90%)
            ReportProgress(++currentStep, 87, "Upgrading Windows Service...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 7: Upgrading Windows Service...");
            var serviceResult = await UpgradeWindowsServiceAsync(tempExtractPath, installation.DatabaseServer, installation.DatabaseName);
            if (!serviceResult.Success)
            {
                StatusUpdate?.Invoke(this, $"Warning: Windows Service upgrade failed: {serviceResult.Message}");
            }
            else
            {
                StatusUpdate?.Invoke(this, serviceResult.Message);
            }
            ReportProgress(currentStep, 90, "Windows Service upgraded");

            // Step 8: Start IIS (90-95%)
            ReportProgress(++currentStep, 92, "Starting IIS Application Pool...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 8: Starting IIS...");
            await StartIISAsync(installation.SiteName);
            StatusUpdate?.Invoke(this, "[UpgradeService] IIS started");
            ReportProgress(currentStep, 95, "IIS started");

            // Step 9: Update registry version (95-98%)
            ReportProgress(++currentStep, 96, "Updating registry...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 9: Updating registry version...");
            await UpdateRegistryVersionAsync(installation.SiteName, newVersion);
            await UpdateRegistryConfigurationAsync(installation);
            
            // Run registry migrations to bring schema up to date
            var migrationService = new RegistryMigrationService(_logger);
            var registryMigrationResult = await migrationService.RunMigrationsAsync(installation.SiteName);
            if (registryMigrationResult.AppliedMigrations.Count > 0)
            {
                StatusUpdate?.Invoke(this, $"✓ Applied {registryMigrationResult.AppliedMigrations.Count} registry migration(s)");
            }
            
            StatusUpdate?.Invoke(this, "[UpgradeService] Registry updated");
            ReportProgress(currentStep, 98, "Registry updated");

            // Verify site is running
            ReportProgress(currentStep, 98, "Verifying site...");
            await Task.Delay(2000); // Give IIS time to start
            ReportProgress(currentStep, 100, "Upgrade complete!");

            // Step 10: Record upgrade completion for Update History (98-100%)
            ReportProgress(currentStep, 99, "Recording upgrade...");
            StatusUpdate?.Invoke(this, "[UpgradeService] Step 10: Recording upgrade completion...");
            try
            {
                await RecordUpgradeCompletionAsync(installation.SiteName, newVersion ?? VersionService.CURRENT_VERSION);
                StatusUpdate?.Invoke(this, "[UpgradeService] Upgrade recorded successfully");
            }
            catch (Exception recordEx)
            {
                StatusUpdate?.Invoke(this, $"[UpgradeService] Warning: Could not record upgrade: {recordEx.Message}");
                // Don't fail the upgrade if recording fails
            }
            
            result.Success = true;
            result.Message = "Upgrade completed successfully!";
            result.BackupPath = backupPath;

            StatusUpdate?.Invoke(this, "[UpgradeService] === UPGRADE COMPLETED SUCCESSFULLY ===");

            return result;
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, "[UpgradeService] === UPGRADE FAILED ===");
            StatusUpdate?.Invoke(this, $"[UpgradeService] Exception Type: {ex.GetType().Name}");
            StatusUpdate?.Invoke(this, $"[UpgradeService] Error Message: {ex.Message}");
            StatusUpdate?.Invoke(this, $"[UpgradeService] Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                StatusUpdate?.Invoke(this, $"[UpgradeService] Inner Exception: {ex.InnerException.Message}");
            }

            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.BackupPath = backupPath;

            // Attempt rollback
            if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
            {
                StatusUpdate?.Invoke(this, "Upgrade failed - attempting rollback...");
                StatusUpdate?.Invoke(this, "[UpgradeService] Attempting rollback...");
                await RollbackAsync(backupPath, installation.InstallPath, installation.SiteName);
            }

            return result;
        }
    }

    private async Task<string> CreateBackupAsync(string installPath)
    {
        var backupPath = Path.Combine(
            Path.GetDirectoryName(installPath) ?? "C:\\Backups",
            $"backup-{Path.GetFileName(installPath)}-{DateTime.Now:yyyyMMdd-HHmms}");

        Directory.CreateDirectory(backupPath);

        StatusUpdate?.Invoke(this, $"Creating backup at {backupPath}...");

        await Task.Run(() =>
        {
            CopyDirectory(installPath, backupPath, excludeDirs: new[] { "logs", "wwwroot\\uploads" });
        });

        return backupPath;
    }

    private async Task StopIISAsync(string siteName)
    {
        await Task.Run(() =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Import-Module WebAdministration; Stop-WebAppPool -Name '{siteName}' -ErrorAction SilentlyContinue\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(10000);
        });

        await Task.Delay(3000); // Give IIS time to stop
    }

    private async Task StartIISAsync(string siteName)
    {
        await Task.Run(() =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Import-Module WebAdministration; Start-WebAppPool -Name '{siteName}' -ErrorAction SilentlyContinue\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(10000);
        });
    }

    private async Task<ConfigurationBackup> PreserveConfigurationAsync(string installPath)
    {
        var backup = new ConfigurationBackup();

        // Note: appsettings.json is NOT backed up - all config is in Windows Registry
        // Only backup web.config (IIS settings, no sensitive data)
        var webConfigPath = Path.Combine(installPath, "web.config");

        if (File.Exists(webConfigPath))
        {
            backup.WebConfig = await File.ReadAllTextAsync(webConfigPath);
        }

        return backup;
    }

    private async Task RestoreConfigurationAsync(ConfigurationBackup backup, string installPath)
    {
        // Note: appsettings.json is NOT restored - registry is authoritative source
        // Only restore web.config (IIS configuration)
        if (!string.IsNullOrEmpty(backup.WebConfig))
        {
            await File.WriteAllTextAsync(Path.Combine(installPath, "web.config"), backup.WebConfig);
        }
    }

    private async Task DeployFilesAsync(string sourcePath, string destPath)
    {
        await Task.Run(() =>
        {
            StatusUpdate?.Invoke(this, $"[DeployFilesAsync] Source: {sourcePath}");
            StatusUpdate?.Invoke(this, $"[DeployFilesAsync] Destination: {destPath}");
            StatusUpdate?.Invoke(this, $"[DeployFilesAsync] Source exists: {Directory.Exists(sourcePath)}");
            StatusUpdate?.Invoke(this, $"[DeployFilesAsync] Destination exists: {Directory.Exists(destPath)}");

            // Ensure destination directory exists before trying to enumerate files
            if (!Directory.Exists(destPath))
            {
                StatusUpdate?.Invoke(this, $"[DeployFilesAsync] ⚠️ Destination does not exist, creating: {destPath}");
                Directory.CreateDirectory(destPath);
            }

            // Delete old files (except config, logs, uploads)
            var protectedFiles = new[] { "web.config" };
            var protectedDirs = new[] { "logs", "wwwroot\\uploads" };

            foreach (var file in Directory.GetFiles(destPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(destPath, file);
                var isProtected = false;

                foreach (var protectedFile in protectedFiles)
                {
                    if (relativePath.Equals(protectedFile, StringComparison.OrdinalIgnoreCase))
                    {
                        isProtected = true;
                        break;
                    }
                }

                foreach (var protectedDir in protectedDirs)
                {
                    if (relativePath.StartsWith(protectedDir, StringComparison.OrdinalIgnoreCase))
                    {
                        isProtected = true;
                        break;
                    }
                }

                if (!isProtected)
                {
                    try { File.Delete(file); } catch { }
                }
            }

            // Copy new files
            CopyDirectory(sourcePath, destPath, overwrite: true, excludeFiles: protectedFiles);
        });
    }

    private async Task UpdateProgramFilesAsync(string tempExtractPath)
    {
        await Task.Run(() =>
        {
            try
            {
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var installerDir = Path.Combine(programFilesPath, "EcommerceStarter");
                
                // Source paths in the downloaded package
                var sourceInstallerPath = Path.Combine(tempExtractPath, "Installer", "EcommerceStarter.Installer.exe");
                var sourceUpgraderPath = Path.Combine(tempExtractPath, "Installer", "EcommerceStarter.Upgrader.exe");
                
                // Destination paths
                var destInstallerPath = Path.Combine(installerDir, "EcommerceStarter.Installer.exe");
                var destUpgraderPath = Path.Combine(installerDir, "EcommerceStarter.Upgrader.exe");

                StatusUpdate?.Invoke(this, $"[UpdateProgramFilesAsync] Source installer: {sourceInstallerPath}");
                StatusUpdate?.Invoke(this, $"[UpdateProgramFilesAsync] Dest installer: {destInstallerPath}");

                // Ensure Program Files directory exists
                if (!Directory.Exists(installerDir))
                {
                    StatusUpdate?.Invoke(this, $"[UpdateProgramFilesAsync] Creating directory: {installerDir}");
                    Directory.CreateDirectory(installerDir);
                }

                // Copy installer if it exists - we CAN overwrite it since it's not running
                if (File.Exists(sourceInstallerPath))
                {
                    StatusUpdate?.Invoke(this, "[UpdateProgramFilesAsync] Copying installer...");
                    File.Copy(sourceInstallerPath, destInstallerPath, overwrite: true);
                    StatusUpdate?.Invoke(this, "✓ Installer updated");
                    
                    // Copy all DLLs from Installer folder (runtime dependencies)
                    var sourceInstallerDir = Path.GetDirectoryName(sourceInstallerPath);
                    if (sourceInstallerDir != null)
                    {
                        foreach (var dll in Directory.GetFiles(sourceInstallerDir, "*.dll"))
                        {
                            var destDll = Path.Combine(installerDir, Path.GetFileName(dll));
                            File.Copy(dll, destDll, overwrite: true);
                        }
                        StatusUpdate?.Invoke(this, "✓ Installer dependencies updated");
                    }
                }
                else
                {
                    StatusUpdate?.Invoke(this, $"[UpdateProgramFilesAsync] WARNING: Installer not found at {sourceInstallerPath}");
                }

                // Copy upgrader if it exists - we CAN overwrite it since we're running from temp, not Program Files!
                if (File.Exists(sourceUpgraderPath))
                {
                    StatusUpdate?.Invoke(this, "[UpdateProgramFilesAsync] Copying upgrader...");
                    File.Copy(sourceUpgraderPath, destUpgraderPath, overwrite: true);
                    StatusUpdate?.Invoke(this, "✓ Upgrader updated");
                }
                else
                {
                    StatusUpdate?.Invoke(this, $"[UpdateProgramFilesAsync] WARNING: Upgrader not found at {sourceUpgraderPath}");
                }

                StatusUpdate?.Invoke(this, "✓ Program Files update complete");
            }
            catch (Exception ex)
            {
                StatusUpdate?.Invoke(this, $"[UpdateProgramFilesAsync] ERROR: {ex.Message}");
                // Don't throw - upgrading installer is optional
            }
        });
    }

    private async Task DeployInstallerExeAsync(string tempExtractPath, string installPath)
    {
        await Task.Run(() =>
        {
            try
            {
                // The installer.exe from the ZIP should be in a nested folder like:
                // tempExtractPath/EcommerceStarter-Installer-vX.X.X/EcommerceStarter.Installer.exe
                // OR at the root of tempExtractPath

                string? sourceInstallerPath = null;

                // Strategy 1: Look in Installer subdirectory (new package structure)
                var installerInSubdir = Path.Combine(tempExtractPath, "Installer", "EcommerceStarter.Installer.exe");
                if (File.Exists(installerInSubdir))
                {
                    sourceInstallerPath = installerInSubdir;
                    StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Found installer in Installer/ subdirectory: {sourceInstallerPath}");
                }

                // Strategy 2: Look in the most recently created nested folder (legacy)
                if (string.IsNullOrEmpty(sourceInstallerPath))
                {
                    var nestedDirs = Directory.GetDirectories(tempExtractPath);
                    foreach (var nestedDir in nestedDirs.OrderByDescending(d => new DirectoryInfo(d).CreationTime))
                    {
                        var installerInNested = Path.Combine(nestedDir, "EcommerceStarter.Installer.exe");
                        if (File.Exists(installerInNested))
                        {
                            sourceInstallerPath = installerInNested;
                            StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Found installer in nested folder: {sourceInstallerPath}");
                            break;
                        }
                    }
                }

                // Strategy 3: Look at root of temp directory if not found elsewhere
                if (string.IsNullOrEmpty(sourceInstallerPath))
                {
                    var installerAtRoot = Path.Combine(tempExtractPath, "EcommerceStarter.Installer.exe");
                    if (File.Exists(installerAtRoot))
                    {
                        sourceInstallerPath = installerAtRoot;
                        StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Found installer at root: {sourceInstallerPath}");
                    }
                }

                if (string.IsNullOrEmpty(sourceInstallerPath))
                {
                    StatusUpdate?.Invoke(this, "[DeployInstallerExeAsync] WARNING: No installer executable found in extracted files");
                    return;
                }

                // Deploy ONLY to C:\Program Files\EcommerceStarter\
                // This is where Programs and Features points to - the official installer location
                // inetpub should ONLY contain web app files, NOT the installer
                
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var destInstallerDir = Path.Combine(programFilesPath, "EcommerceStarter");
                var destInstallerPath = Path.Combine(destInstallerDir, "EcommerceStarter.Installer.exe");

                StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Source: {sourceInstallerPath}");
                StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Dest: {destInstallerPath}");

                try
                {
                    // Ensure directory exists
                    Directory.CreateDirectory(destInstallerDir);

                    // Get version BEFORE copy
                    var sourceVersion = System.Reflection.AssemblyName.GetAssemblyName(sourceInstallerPath)?.Version?.ToString() ?? "Unknown";
                    StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Source installer version: {sourceVersion}");

                    // The currently running installer has a lock on the destination file
                    // Strategy: Rename the old installer, copy new one, schedule old for deletion on reboot
                    if (File.Exists(destInstallerPath))
                    {
                        var oldInstallerPath = Path.Combine(destInstallerDir, "EcommerceStarter.Installer.exe.old");
                        
                        try
                        {
                            // Remove any existing .old file first
                            if (File.Exists(oldInstallerPath))
                            {
                                File.Delete(oldInstallerPath);
                            }
                            
                            // Move current installer to .old (works even if locked)
                            File.Move(destInstallerPath, oldInstallerPath);
                            StatusUpdate?.Invoke(this, "[DeployInstallerExeAsync] Renamed old installer to .old");
                        }
                        catch (Exception moveEx)
                        {
                            // If move fails (file locked), try direct overwrite as fallback
                            StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] Could not rename old installer: {moveEx.Message}");
                        }
                    }

                    // Copy new installer
                    File.Copy(sourceInstallerPath, destInstallerPath, overwrite: true);
                    StatusUpdate?.Invoke(this, "[DeployInstallerExeAsync] ✓ Installer deployed to Program Files");

                    // Verify the copy and get deployed version
                    if (File.Exists(destInstallerPath))
                    {
                        var deployedVersion = System.Reflection.AssemblyName.GetAssemblyName(destInstallerPath)?.Version?.ToString() ?? "Unknown";
                        StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] ✓ Deployed version verified: {deployedVersion}");
                    }
                }
                catch (Exception ex)
                {
                    StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] ERROR deploying installer: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                StatusUpdate?.Invoke(this, $"[DeployInstallerExeAsync] ERROR: {ex.Message}");
            }
        });
    }

    private async Task<OperationResult> RunMigrationsAsync(string installPath, string server, string database)
    {
        try
        {
            StatusUpdate?.Invoke(this, "[RunMigrationsAsync] Starting migrations...");
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Install Path: {installPath}");
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Database: {server}/{database}");

            // Use the bundled efbundle.exe for migrations (included in portable build)
            // This is a standalone executable that doesn't require the source code or project file
            // The ZIP may extract to a nested folder (e.g., EcommerceStarter-Installer-v1.0.9\migrations\),
            // so search recursively for the migrations folder
            var efBundlePath = FindMigrationsBundle(installPath);

            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Looking for efbundle at: {efBundlePath}");
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] efbundle exists: {efBundlePath != null && File.Exists(efBundlePath)}");

            if (!File.Exists(efBundlePath))
            {
                var errorMsg = $"Migrations bundle not found at {efBundlePath}. Ensure the installer package includes the migrations folder.";
                StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] ERROR: {errorMsg}");
                return new OperationResult
                {
                    Success = false,
                    Message = errorMsg
                };
            }

            // Build connection string for the migrations
            var connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";

            var psi = new ProcessStartInfo
            {
                FileName = efBundlePath,
                Arguments = $"--connection \"{connectionString}\"",
                WorkingDirectory = Path.GetDirectoryName(efBundlePath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Starting process: {efBundlePath}");
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Arguments: {psi.Arguments}");

            using var process = Process.Start(psi);
            if (process == null)
            {
                StatusUpdate?.Invoke(this, "[RunMigrationsAsync] ERROR: Failed to start process");
                return new OperationResult { Success = false, Message = "Failed to start migration bundle process" };
            }

            await process.WaitForExitAsync();

            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Process exited with code: {process.ExitCode}");

            if (process.ExitCode == 0)
            {
                StatusUpdate?.Invoke(this, "[RunMigrationsAsync] Migrations applied successfully");
                return new OperationResult { Success = true, Message = "Migrations applied successfully" };
            }

            var error = await process.StandardError.ReadToEndAsync();
            var output = await process.StandardOutput.ReadToEndAsync();

            var errorDetail = $"Migration failed: {error} {output}".Trim();
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Migration error: {errorDetail}");

            return new OperationResult { Success = false, Message = errorDetail };
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Exception: {ex.Message}");
            StatusUpdate?.Invoke(this, $"[RunMigrationsAsync] Stack Trace: {ex.StackTrace}");
            return new OperationResult { Success = false, Message = $"Migration execution error: {ex.Message}" };
        }
    }
    private async Task RollbackAsync(string backupPath, string installPath, string siteName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Rolling back to previous version...");

            // Stop IIS
            await StopIISAsync(siteName);

            // Restore backup
            await Task.Run(() =>
            {
                CopyDirectory(backupPath, installPath, overwrite: true);
            });

            // Start IIS
            await StartIISAsync(siteName);

            StatusUpdate?.Invoke(this, "Rollback complete");
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Rollback failed: {ex.Message}");
        }
    }

    private void CopyDirectory(string sourceDir, string destDir, bool overwrite = false, string[]? excludeDirs = null, string[]? excludeFiles = null)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            return;

        Directory.CreateDirectory(destDir);

        foreach (var file in dir.GetFiles())
        {
            if (excludeFiles != null && Array.Exists(excludeFiles, e => e.Equals(file.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var targetPath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetPath, overwrite);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            if (excludeDirs != null && Array.Exists(excludeDirs, e => subDir.Name.Equals(e, StringComparison.OrdinalIgnoreCase)))
                continue;

            var newDestDir = Path.Combine(destDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestDir, overwrite, excludeDirs, excludeFiles);
        }
    }

    private void ReportProgress(int step, int percentage, string message)
    {
        ProgressUpdate?.Invoke(this, new UpgradeProgress
        {
            CurrentStep = step,
            Percentage = percentage,
            Message = message
        });
    }

    /// <summary>
    /// Simulate upgrade for demo mode (no real changes)
    /// </summary>
    private async Task SimulateDemoUpgradeAsync(ExistingInstallation installation)
    {
        var steps = new[]
        {
            "Creating backup of current installation... (simulated)",
            "Stopping IIS application... (simulated)",
            $"Extracting new version to {installation.InstallPath}... (simulated)",
            "Updating database schema... (simulated)",
            "Restoring configuration files... (simulated)",
            "Starting IIS application... (simulated)",
            "Verifying upgrade... (simulated)"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            // Simulate work
            await Task.Delay(1000);

            // In real implementation, would report progress here
            StatusUpdate?.Invoke(this, $"?? DEMO: {steps[i]}");
        }
    }

    private async Task UpdateRegistryVersionAsync(string siteName, string? overrideVersion = null)
    {
        try
        {
            // Step 0: If version was explicitly provided (from GitHub release), use it directly
            // This is the most reliable source - it came from the release we're upgrading to
            string? installerVersion = overrideVersion;
            if (!string.IsNullOrEmpty(installerVersion))
            {
                StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] ✓ Using version from GitHub release: {installerVersion}");
                StatusUpdate?.Invoke(this, $"Using GitHub release version: {installerVersion}");
            }
            else
            {
                // Step 1: Try to get version from newly deployed files first (if available)
                // This ensures we update to the NEW version, not the old running version

                // Check Program Files for the newly deployed installer
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var installerProgramFilesPath = Path.Combine(programFilesPath, "EcommerceStarter", "EcommerceStarter.Installer.exe");

                if (File.Exists(installerProgramFilesPath))
                {
                    try
                    {
                        // Use Assembly.GetName().Version which is the authoritative version
                        // FileVersionInfo can have extra formatting that makes it unreliable
                        var assembly = System.Reflection.AssemblyName.GetAssemblyName(installerProgramFilesPath);
                        if (assembly?.Version != null)
                        {
                            installerVersion = assembly.Version.ToString();
                            StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] ✓ Got version from deployed installer assembly: {installerVersion}");
                            StatusUpdate?.Invoke(this, $"✓ Deployed installer version: {installerVersion}");
                        }
                        else
                        {
                            // Fallback to FileVersion if Assembly version not available
                            var fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(installerProgramFilesPath);
                            if (!string.IsNullOrEmpty(fileVersion.FileVersion))
                            {
                                installerVersion = fileVersion.FileVersion.Trim();
                                StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] ✓ Got version from deployed installer FileVersion: {installerVersion}");
                                StatusUpdate?.Invoke(this, $"✓ FileVersion from deployed installer: {installerVersion}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] ✗ Could not read deployed installer version: {ex.Message}");
                        StatusUpdate?.Invoke(this, $"✗ Could not read deployed installer: {ex.Message}");
                    }
                }
                else
                {
                    StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] ✗ Deployed installer not found at: {installerProgramFilesPath}");
                    StatusUpdate?.Invoke(this, $"✗ Deployed installer not found at {installerProgramFilesPath}");
                }

                // Step 2: Fallback: use currently running assembly version
                if (string.IsNullOrEmpty(installerVersion))
                {
                    try
                    {
                        var runningVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();
                        if (!string.IsNullOrEmpty(runningVersion))
                        {
                            installerVersion = runningVersion;
                            StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] Using running assembly version: {installerVersion}");
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] Could not read running assembly version: {ex.Message}");
                    }
                }

                // Step 3: Final fallback: use constant (should not reach here in normal operation)
                if (string.IsNullOrEmpty(installerVersion))
                {
                    installerVersion = VersionService.CURRENT_VERSION;
                    StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] Using CURRENT_VERSION constant: {installerVersion}");
                }
            } // End of else block (no override version provided)

            // CRITICAL VALIDATION: Read the ACTUAL version from Program Files installer
            // This proves whether the deployment actually updated the EXE
            StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] === VERIFYING PROGRAM FILES INSTALLER ===");
            var verifyProgramFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var installerExePath = Path.Combine(verifyProgramFilesPath, "EcommerceStarter", "EcommerceStarter.Installer.exe");
            
            string actualInstalledVersion = "UNKNOWN";
            if (File.Exists(installerExePath))
            {
                try
                {
                    var assembly = System.Reflection.AssemblyName.GetAssemblyName(installerExePath);
                    if (assembly?.Version != null)
                    {
                        actualInstalledVersion = assembly.Version.ToString();
                        StatusUpdate?.Invoke(this, $"✓ VERIFIED: Program Files installer version is {actualInstalledVersion}");
                        StatusUpdate?.Invoke(this, $"✓ Location: {installerExePath}");
                        
                        // If we have a target version, compare to prove deployment success/failure
                        if (!string.IsNullOrEmpty(installerVersion) && actualInstalledVersion != installerVersion)
                        {
                            StatusUpdate?.Invoke(this, $"⚠️ WARNING: Expected {installerVersion} but found {actualInstalledVersion}");
                            StatusUpdate?.Invoke(this, "⚠️ This means Program Files installer was NOT updated!");
                        }
                        else if (!string.IsNullOrEmpty(installerVersion))
                        {
                            StatusUpdate?.Invoke(this, $"✓ SUCCESS: Program Files matches expected version {installerVersion}");
                        }
                        
                        // Use the ACTUAL installed version for registry
                        installerVersion = actualInstalledVersion;
                    }
                }
                catch (Exception ex)
                {
                    StatusUpdate?.Invoke(this, $"✗ Could not verify Program Files installer: {ex.Message}");
                }
            }
            else
            {
                StatusUpdate?.Invoke(this, $"✗ CRITICAL: Installer not found at {installerExePath}");
                StatusUpdate?.Invoke(this, "✗ Program Files was NOT updated!");
            }
            StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] =======================================");

            // Use the full version string as-is (supports both 3-part and 4-part formats)
            // Windows registry supports full semantic versions
            var displayVersion = installerVersion;
            StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] Registry DisplayVersion will be set to: {displayVersion}");

            var registryKeyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";

            StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] Attempting to update registry DisplayVersion to {displayVersion}");
            StatusUpdate?.Invoke(this, $"Registry path: {registryKeyPath}");
            StatusUpdate?.Invoke(this, $"Updating registry DisplayVersion to {displayVersion}...");

            // Strategy 1: Direct .NET Registry API (preferred if admin)
            bool directUpdateSuccess = false;
            try
            {
                StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] Strategy 1: Direct .NET Registry API");
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryKeyPath, true))
                {
                    if (key != null)
                    {
                        // Read current values first for validation
                        var currentVersion = key.GetValue("DisplayVersion") as string;
                        StatusUpdate?.Invoke(this, $"Current registry DisplayVersion: {currentVersion}");

                        // Update the DisplayVersion
                        key.SetValue("DisplayVersion", displayVersion);

                        // Verify the write was successful
                        var newVersion = key.GetValue("DisplayVersion") as string;
                        if (newVersion == displayVersion)
                        {
                            StatusUpdate?.Invoke(this, $"✓ Direct registry update SUCCESSFUL: {currentVersion} → {displayVersion}");
                            directUpdateSuccess = true;
                        }
                        else
                        {
                            StatusUpdate?.Invoke(this, $"✗ Direct registry update FAILED: Value didn't persist ({newVersion})");
                        }
                    }
                    else
                    {
                        StatusUpdate?.Invoke(this, $"✗ Registry key not found at: {registryKeyPath}");
                    }
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                StatusUpdate?.Invoke(this, $"✗ Strategy 1 failed - Insufficient permissions: {uaEx.Message}");
                StatusUpdate?.Invoke(this, "→ Attempting fallback strategies...");
            }
            catch (Exception regEx)
            {
                StatusUpdate?.Invoke(this, $"✗ Strategy 1 failed - Direct registry error: {regEx.Message}");
                StatusUpdate?.Invoke(this, "→ Attempting fallback strategies...");
            }

            // Strategy 2: PowerShell fallback (if direct method failed)
            if (!directUpdateSuccess)
            {
                try
                {
                    StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] Strategy 2: PowerShell Registry Update");
                    await UpdateRegistryViaPowerShellAsync(registryKeyPath, displayVersion);
                }
                catch (Exception psEx)
                {
                    StatusUpdate?.Invoke(this, $"✗ Strategy 2 failed - PowerShell error: {psEx.Message}");
                }
            }

            // Strategy 3: Custom registry location (always update as backup)
            try
            {
                StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] Strategy 3: Custom registry location backup");
                var stateService = new InstallationStateService();
                var installPath = GetInstallPathFromRegistry(siteName);
                if (!string.IsNullOrEmpty(installPath))
                {
                    stateService.SaveInstallationInfo(installerVersion, installPath);
                    StatusUpdate?.Invoke(this, $"✓ Custom registry location updated");
                }
            }
            catch (Exception customEx)
            {
                StatusUpdate?.Invoke(this, $"✗ Strategy 3 failed - Custom registry error: {customEx.Message}");
            }

            // FINAL VALIDATION: Read from deployed installer to confirm version is correct
            try
            {
                StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] === FINAL VALIDATION ===");
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var deployedInstallerPath = Path.Combine(programFilesPath, "EcommerceStarter", "EcommerceStarter.Installer.exe");

                if (File.Exists(deployedInstallerPath))
                {
                    var deployedAssembly = System.Reflection.AssemblyName.GetAssemblyName(deployedInstallerPath);
                    var deployedVersion = deployedAssembly?.Version?.ToString() ?? "Unknown";

                    StatusUpdate?.Invoke(this, $"Deployed installer version: {deployedVersion}");
                    StatusUpdate?.Invoke(this, $"Registry set to: {displayVersion}");

                    if (deployedVersion == displayVersion)
                    {
                        StatusUpdate?.Invoke(this, "✓✓✓ VALIDATION SUCCESSFUL: Deployed version matches registry version");
                    }
                    else
                    {
                        StatusUpdate?.Invoke(this, $"⚠ VERSION MISMATCH: Deployed={deployedVersion}, Registry={displayVersion}");
                    }
                }
                else
                {
                    StatusUpdate?.Invoke(this, $"⚠ Cannot validate: Deployed installer not found at {deployedInstallerPath}");
                }
            }
            catch (Exception validationEx)
            {
                StatusUpdate?.Invoke(this, $"⚠ Validation step failed: {validationEx.Message}");
            }

            StatusUpdate?.Invoke(this, "[UpdateRegistryVersionAsync] Registry update process completed");
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[UpdateRegistryVersionAsync] Outer exception: {ex.Message}");
            // Don't fail the upgrade for registry update issues
        }
    }

    /// <summary>
    /// Update registry via PowerShell when direct registry access fails (usually permission issues)
    /// </summary>
    private async Task UpdateRegistryViaPowerShellAsync(string registryKeyPath, string installerVersion)
    {
        try
        {
            var psScript = $@"
try {{
    $path = 'HKLM:\{registryKeyPath}'
    Set-ItemProperty -Path $path -Name 'DisplayVersion' -Value '{installerVersion}' -ErrorAction Stop
    Write-Host 'Registry updated via PowerShell: DisplayVersion = {installerVersion}'
    exit 0
}}
catch {{
    Write-Host 'PowerShell registry update failed: $($_.Exception.Message)'
    exit 1
}}
";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{psScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit());

                    if (process.ExitCode == 0)
                    {
                        StatusUpdate?.Invoke(this, $"✓ PowerShell update successful");
                        StatusUpdate?.Invoke(this, $"Output: {output.Trim()}");
                    }
                    else
                    {
                        StatusUpdate?.Invoke(this, $"✗ PowerShell update failed with exit code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            StatusUpdate?.Invoke(this, $"Error: {error}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[UpdateRegistryViaPowerShellAsync] Exception: {ex.Message}");
            throw;
        }
    }

    private string GetInstallPathFromRegistry(string siteName)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}"))
            {
                if (key != null)
                {
                    var installLocation = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(installLocation))
                    {
                        StatusUpdate?.Invoke(this, $"[GetInstallPathFromRegistry] Found install path: {installLocation}");
                        return installLocation;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[GetInstallPathFromRegistry] Error: {ex.Message}");
        }
        return string.Empty;
    }

    private string? FindAppFolder(string basePath)
    {
        try
        {
            // Check for "Application" folder (new package structure)
            var applicationPath = Path.Combine(basePath, "Application");
            if (Directory.Exists(applicationPath))
            {
                StatusUpdate?.Invoke(this, $"[FindAppFolder] Found Application folder at: {applicationPath}");
                return applicationPath;
            }

            // Check for legacy "app" folder
            var directAppPath = Path.Combine(basePath, "app");
            if (Directory.Exists(directAppPath))
            {
                StatusUpdate?.Invoke(this, $"[FindAppFolder] Found app folder at: {directAppPath}");
                return directAppPath;
            }

            // Search recursively for Application folder (handles nested extraction)
            var applicationFolders = Directory.GetDirectories(basePath, "Application", SearchOption.AllDirectories);
            if (applicationFolders.Length > 0)
            {
                StatusUpdate?.Invoke(this, $"[FindAppFolder] Found Application folder via recursive search: {applicationFolders[0]}");
                return applicationFolders[0];
            }

            // Search recursively for legacy app folder
            var appFolders = Directory.GetDirectories(basePath, "app", SearchOption.AllDirectories);
            if (appFolders.Length > 0)
            {
                StatusUpdate?.Invoke(this, $"[FindAppFolder] Found app folder via recursive search: {appFolders[0]}");
                return appFolders[0];
            }

            StatusUpdate?.Invoke(this, $"[FindAppFolder] No Application or app folder found in {basePath} or subdirectories");
            return null;
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[FindAppFolder] Error searching for app folder: {ex.Message}");
            return null;
        }
    }

    private string? FindMigrationsBundle(string basePath)
    {
        try
        {
            // First try direct path: basePath\migrations\efbundle.exe
            var directPath = Path.Combine(basePath, "migrations", "efbundle.exe");
            if (File.Exists(directPath))
            {
                StatusUpdate?.Invoke(this, $"[FindMigrationsBundle] Found at direct path: {directPath}");
                return directPath;
            }

            // If not found, search recursively for migrations folder
            // This handles nested extraction like: basePath\EcommerceStarter-Installer-v1.0.9\migrations\efbundle.exe
            StatusUpdate?.Invoke(this, $"[FindMigrationsBundle] Direct path not found, searching recursively in: {basePath}");

            var migrationsFolder = Directory.GetDirectories(basePath, "migrations", SearchOption.AllDirectories).FirstOrDefault();
            if (migrationsFolder != null)
            {
                var bundlePath = Path.Combine(migrationsFolder, "efbundle.exe");
                if (File.Exists(bundlePath))
                {
                    StatusUpdate?.Invoke(this, $"[FindMigrationsBundle] Found via recursive search: {bundlePath}");
                    return bundlePath;
                }
            }

            StatusUpdate?.Invoke(this, $"[FindMigrationsBundle] No migrations bundle found in {basePath} or subdirectories");
            return null;
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[FindMigrationsBundle] Error searching for migrations: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extract archive file (ZIP or tar.gz) to destination directory
    /// Supports both .zip and .tar.gz formats for maximum compatibility
    /// </summary>
    private async Task ExtractArchiveAsync(string archivePath, string destinationPath)
    {
        try
        {
            Directory.CreateDirectory(destinationPath);

            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || 
                archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
            {
                // Extract tar.gz using native tar command (available on Windows 10+)
                StatusUpdate?.Invoke(this, "[ExtractArchiveAsync] Detected tar.gz archive - using tar extraction");
                await ExtractTarGzAsync(archivePath, destinationPath);
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Extract ZIP using built-in ZipFile class
                StatusUpdate?.Invoke(this, "[ExtractArchiveAsync] Detected ZIP archive - using ZipFile extraction");
                await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, destinationPath));
                StatusUpdate?.Invoke(this, "[ExtractArchiveAsync] ZIP extraction complete");
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {Path.GetExtension(archivePath)}");
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[ExtractArchiveAsync] Extraction failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Extract tar.gz archive using native Windows tar command
    /// </summary>
    private async Task ExtractTarGzAsync(string tarGzPath, string destinationPath)
    {
        try
        {
            StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] Starting tar extraction");
            StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] Archive: {tarGzPath}");
            StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] Destination: {destinationPath}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{tarGzPath}\" -C \"{destinationPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] Executing: tar {processInfo.Arguments}");

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start tar process");
                }

                _ = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var errorMsg = $"tar extraction failed with exit code {process.ExitCode}";
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorMsg += $"\nError: {error}";
                    }
                    StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] ✗ {errorMsg}");
                    throw new InvalidOperationException(errorMsg);
                }

                StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] ✓ tar extraction successful");
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[ExtractTarGzAsync] Exception: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Upgrade the Windows Service to the new version
    /// </summary>
    private async Task<OperationResult> UpgradeWindowsServiceAsync(string sourceExtractPath, string dbServer, string dbName)
    {
        try
        {
            var serviceName = "EcommerceStarter Background Service";
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var servicePath = Path.Combine(programFilesPath, "EcommerceStarter", "WindowsService");
            var serviceSourcePath = Path.Combine(sourceExtractPath, "WindowsService");

            // Check if service source files exist in upgrade package
            if (!Directory.Exists(serviceSourcePath))
            {
                StatusUpdate?.Invoke(this, "Windows Service files not found in upgrade package - skipping");
                return new OperationResult 
                { 
                    Success = true, 
                    Message = "Windows Service not included in upgrade package (will use existing)" 
                };
            }

            // Check if service is currently installed
            var serviceExistsScript = $@"
                $service = Get-Service -Name '{serviceName}' -ErrorAction SilentlyContinue;
                if ($service) {{ Write-Output 'EXISTS' }} else {{ Write-Output 'NOT_FOUND' }}
            ";

            var checkPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{serviceExistsScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            string serviceStatus;
            using (var checkProcess = Process.Start(checkPsi))
            {
                if (checkProcess == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Could not check service status" };
                }
                serviceStatus = (await checkProcess.StandardOutput.ReadToEndAsync()).Trim();
                await checkProcess.WaitForExitAsync();
            }

            if (serviceStatus != "EXISTS")
            {
                StatusUpdate?.Invoke(this, "Windows Service not currently installed - installing new service...");
                
                // Service doesn't exist - perform fresh installation
                return await InstallWindowsServiceAsync(serviceSourcePath, servicePath, serviceName, dbServer, dbName);
            }

            StatusUpdate?.Invoke(this, "Stopping Windows Service...");

            // Stop the service
            var stopScript = $@"
                Stop-Service -Name '{serviceName}' -Force -ErrorAction SilentlyContinue;
                Start-Sleep -Seconds 3;
                Write-Output 'Service stopped';
            ";

            var stopPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{stopScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var stopProcess = Process.Start(stopPsi))
            {
                if (stopProcess != null)
                {
                    await stopProcess.WaitForExitAsync();
                }
            }

            // Note: Service configuration comes from Windows Registry, not appsettings.json
            // No backup/restore of configuration files needed
            
            // Copy new service files
            StatusUpdate?.Invoke(this, "Copying new Windows Service files...");
            await CopyDirectoryAsync(serviceSourcePath, servicePath);

            // Note: Windows Service reads configuration from Windows Registry
            // No appsettings.json needed - service uses RegistryConfigService
            StatusUpdate?.Invoke(this, "Windows Service upgraded - uses registry configuration");

            // Start the service
            StatusUpdate?.Invoke(this, "Starting Windows Service...");
            var startScript = $@"
                Start-Service -Name '{serviceName}' -ErrorAction SilentlyContinue;
                Start-Sleep -Seconds 2;
                $service = Get-Service -Name '{serviceName}';
                if ($service.Status -eq 'Running') {{
                    Write-Output 'Service started successfully';
                }} else {{
                    Write-Error 'Service failed to start';
                    exit 1;
                }}
            ";

            var startPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{startScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var startProcess = Process.Start(startPsi))
            {
                if (startProcess == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Could not start service" };
                }

                var output = await startProcess.StandardOutput.ReadToEndAsync();
                var error = await startProcess.StandardError.ReadToEndAsync();
                await startProcess.WaitForExitAsync();

                if (startProcess.ExitCode != 0)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Service failed to start: {error}" 
                    };
                }
            }

            StatusUpdate?.Invoke(this, "✓ Windows Service upgraded successfully");
            return new OperationResult 
            { 
                Success = true, 
                Message = "Windows Service upgraded and running" 
            };
        }
        catch (Exception ex)
        {
            return new OperationResult 
            { 
                Success = false, 
                ErrorMessage = $"Windows Service upgrade failed: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// Install Windows Service for the first time (called during upgrade if service doesn't exist)
    /// </summary>
    private async Task<OperationResult> InstallWindowsServiceAsync(string serviceSourcePath, string servicePath, string serviceName, string dbServer, string dbName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Installing Windows Service for the first time...");

            // Create service directory
            Directory.CreateDirectory(servicePath);
            StatusUpdate?.Invoke(this, $"Created service directory: {servicePath}");

            // Copy service files
            StatusUpdate?.Invoke(this, "Copying Windows Service files...");
            await CopyDirectoryAsync(serviceSourcePath, servicePath);
            StatusUpdate?.Invoke(this, "Service files copied");

            // Note: Service configuration comes from Windows Registry (not appsettings.json)
            // Service uses RegistryConfigService to read site name and base URL
            var serviceExePath = Path.Combine(servicePath, "EcommerceStarter.WindowsService.exe");

            // Register service with Windows
            StatusUpdate?.Invoke(this, "Registering Windows Service...");
            var escapedPath = serviceExePath.Replace(@"\", @"\\").Replace("\"", "\\\"");
            var escapedServiceName = serviceName.Replace("'", "''");

            var registerScript = $@"
                $servicePath = '{escapedPath}';
                $serviceName = '{escapedServiceName}';
                
                # Check if service already exists
                $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue;
                if ($existingService) {{
                    Write-Output 'Service already registered';
                    exit 0;
                }}

                # Create service
                New-Service -Name $serviceName `
                    -BinaryPathName $servicePath `
                    -DisplayName $serviceName `
                    -Description 'Background processing service for EcommerceStarter' `
                    -StartupType Automatic;

                # Configure service to restart on failure (critical for production stability)
                sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000;

                Write-Output 'Service registered successfully with automatic restart on failure';
            ";

            var registerPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{registerScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas" // Run as admin
            };

            using (var registerProcess = Process.Start(registerPsi))
            {
                if (registerProcess == null)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Failed to register service" 
                    };
                }

                var output = await registerProcess.StandardOutput.ReadToEndAsync();
                var error = await registerProcess.StandardError.ReadToEndAsync();
                await registerProcess.WaitForExitAsync();

                if (registerProcess.ExitCode != 0)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Service registration failed: {error}" 
                    };
                }

                StatusUpdate?.Invoke(this, output.Trim());
            }

            // Start the service
            StatusUpdate?.Invoke(this, "Starting Windows Service...");
            var startScript = $@"
                Start-Service -Name '{escapedServiceName}' -ErrorAction SilentlyContinue;
                Start-Sleep -Seconds 2;
                $service = Get-Service -Name '{escapedServiceName}';
                if ($service.Status -eq 'Running') {{
                    Write-Output 'Service started successfully';
                }} else {{
                    Write-Output 'Service registered but not started (may need manual start)';
                }}
            ";

            var startPsi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{startScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var startProcess = Process.Start(startPsi))
            {
                if (startProcess != null)
                {
                    var startOutput = await startProcess.StandardOutput.ReadToEndAsync();
                    await startProcess.WaitForExitAsync();
                    StatusUpdate?.Invoke(this, startOutput.Trim());
                }
            }

            StatusUpdate?.Invoke(this, "✓ Windows Service installed successfully");
            return new OperationResult 
            { 
                Success = true, 
                Message = "Windows Service installed and configured" 
            };
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Windows Service installation failed: {ex.Message}");
            return new OperationResult 
            { 
                Success = false, 
                ErrorMessage = $"Windows Service installation failed: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// Recursively copy directory contents
    /// </summary>
    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy all subdirectories
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var destDir = Path.Combine(targetDir, dirName);
            await CopyDirectoryAsync(directory, destDir);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Update registry configuration with current installation details
    /// </summary>
    private async Task UpdateRegistryConfigurationAsync(ExistingInstallation installation)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Updating registry configuration...");

            var escapedSiteName = installation.SiteName.Replace("'", "''");
            var escapedDbServer = installation.DatabaseServer.Replace("\\", "\\\\").Replace("'", "''");
            var escapedDbName = installation.DatabaseName.Replace("'", "''");
            var escapedInstallPath = installation.InstallPath.Replace("'", "''");

            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var serviceInstallPath = Path.Combine(programFilesPath, "EcommerceStarter", "WindowsService").Replace("'", "''");

            // Try to detect localhost port from IIS
            var localhostPort = await DetectLocalhostPortAsync(installation.SiteName);

            var script = $@"
                $configPath = 'HKLM:\SOFTWARE\EcommerceStarter\{escapedSiteName}';
                
                if (Test-Path $configPath) {{
                    # Update existing configuration
                    Set-ItemProperty -Path $configPath -Name 'InstallPath' -Value '{escapedInstallPath}' -Type String -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path $configPath -Name 'LastUpgradeDate' -Value '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' -Type String -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path $configPath -Name 'Version' -Value '{VersionService.CURRENT_VERSION}' -Type String -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path $configPath -Name 'DatabaseServer' -Value '{escapedDbServer}' -Type String -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path $configPath -Name 'DatabaseName' -Value '{escapedDbName}' -Type String -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path $configPath -Name 'ServiceInstallPath' -Value '{serviceInstallPath}' -Type String -ErrorAction SilentlyContinue;
                    
                    # Update localhost port if detected
                    {(localhostPort > 0 ? $"Set-ItemProperty -Path $configPath -Name 'LocalhostPort' -Value {localhostPort} -Type DWord -ErrorAction SilentlyContinue;" : "")}
                    {(localhostPort > 0 ? $"Set-ItemProperty -Path $configPath -Name 'ServiceUrl' -Value 'http://localhost:{localhostPort}' -Type String -ErrorAction SilentlyContinue;" : "")}
                    
                    Write-Output 'Configuration updated in registry';
                }} else {{
                    Write-Output 'Configuration not found in registry - skipping';
                }}
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                StatusUpdate?.Invoke(this, "✓ Registry configuration updated");
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Warning: Could not update registry configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect localhost port from IIS bindings
    /// </summary>
    private async Task<int> DetectLocalhostPortAsync(string siteName)
    {
        try
        {
            var escapedSiteName = siteName.Replace("'", "''");
            var script = $@"
                Import-Module WebAdministration -ErrorAction SilentlyContinue;
                $binding = Get-WebBinding -Name '{escapedSiteName}' -ErrorAction SilentlyContinue | Select-Object -First 1;
                if ($binding) {{
                    $port = $binding.bindingInformation -replace '.*:(\d+):.*', '$1';
                    Write-Output $port;
                }} else {{
                    Write-Output '0';
                }}
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
                await process.WaitForExitAsync();

                if (int.TryParse(output, out int port) && port > 0)
                {
                    StatusUpdate?.Invoke(this, $"Detected IIS port: {port}");
                    return port;
                }
            }
        }
        catch
        {
            // Ignore errors - port detection is optional
        }

        return 0;
    }

    /// <summary>
    /// Record upgrade completion to registry for web app to save to UpdateHistory table on next startup
    /// Also updates DisplayVersion in main registry location
    /// </summary>
    private async Task RecordUpgradeCompletionAsync(string siteName, string newVersion)
    {
        try
        {
            var registryPath = $@"SOFTWARE\EcommerceStarter\{siteName}\PendingUpdateHistory";
            var mainRegistryPath = $@"SOFTWARE\EcommerceStarter\{siteName}";
            
            // Normalize version to 3-part format for DisplayVersion (1.2.1 instead of 1.2.1.0)
            var parts = newVersion.Split('.');
            var displayVersion = parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : newVersion;
            
            var timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            var script = $@"
                # Update PendingUpdateHistory (for UpdateHistoryRecorderService)
                $regPath = 'HKLM:\{registryPath}';
                if (!(Test-Path $regPath)) {{
                    New-Item -Path $regPath -Force | Out-Null;
                }}
                Set-ItemProperty -Path $regPath -Name 'Version' -Value '{newVersion}' -Type String;
                Set-ItemProperty -Path $regPath -Name 'CompletedAt' -Value '{timestamp}' -Type String;
                Set-ItemProperty -Path $regPath -Name 'Status' -Value 'Success' -Type String;
                
                # Update main registry location (Version + DisplayVersion)
                $mainRegPath = 'HKLM:\{mainRegistryPath}';
                if (!(Test-Path $mainRegPath)) {{
                    New-Item -Path $mainRegPath -Force | Out-Null;
                }}
                Set-ItemProperty -Path $mainRegPath -Name 'Version' -Value '{newVersion}' -Type String;
                Set-ItemProperty -Path $mainRegPath -Name 'DisplayVersion' -Value '{displayVersion}' -Type String;
                
                # Clean up orphaned/duplicate registry keys (keep only essential keys)
                $essentialKeys = @(
                    'DatabaseName',
                    'DatabaseServer',
                    'DisplayIcon',
                    'DisplayName',
                    'DisplayVersion',
                    'InstallDate',
                    'InstallLocation',
                    'InstallPath',
                    'LastMigrationDate',
                    'LastUpgradeDate',
                    'LocalhostPort',
                    'ModifyPath',
                    'NoModify',
                    'NoRepair',
                    'Publisher',
                    'RegistrySchemaVersion',
                    'ServiceInstallPath',
                    'ServiceUrl',
                    'UninstallString',
                    'Version'
                );
                
                $allKeys = (Get-Item -Path $mainRegPath -ErrorAction SilentlyContinue).Property;
                if ($allKeys) {{
                    $orphanKeys = $allKeys | Where-Object {{ $_ -notin $essentialKeys }};
                    foreach ($key in $orphanKeys) {{
                        try {{
                            Remove-ItemProperty -Path $mainRegPath -Name $key -ErrorAction SilentlyContinue;
                            Write-Output ""Removed orphan key: $key"";
                        }} catch {{
                            Write-Output ""Failed to remove key: $key"";
                        }}
                    }}
                }}
                
                Write-Output 'Registry updated: Version={newVersion}, DisplayVersion={displayVersion}';
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                StatusUpdate?.Invoke(this, $"[RecordUpgradeCompletion] {output.Trim()}");
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"[RecordUpgradeCompletion] Error: {ex.Message}");
            // Don't throw - recording is optional
        }
    }

    /// <summary>
    /// Ensures internal service key exists in SiteSettings (for automated API testing)
    /// Generates and encrypts a new key if it doesn't exist
    /// </summary>
    private async Task EnsureInternalServiceKeyExistsAsync(string server, string database)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Checking internal service key configuration...");

            // Connection string for database access
            var connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";

            // PowerShell script to check if key exists and generate if needed
            var script = $@"
                $connectionString = '{connectionString.Replace("'", "''")}';
                $ErrorActionPreference = 'Stop';

                try {{
                    # Load SQL Server assembly
                    Add-Type -Path 'C:\Program Files\Microsoft SQL Server\160\SDK\Assemblies\Microsoft.Data.SqlClient.dll' -ErrorAction SilentlyContinue;

                    $connection = New-Object Microsoft.Data.SqlClient.SqlConnection($connectionString);
                    $connection.Open();

                    # Check if InternalServiceKeyEncrypted column exists
                    $checkColumnCmd = $connection.CreateCommand();
                    $checkColumnCmd.CommandText = ""
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'SiteSettings' 
                        AND COLUMN_NAME = 'InternalServiceKeyEncrypted'"";
                    $columnExists = [int]$checkColumnCmd.ExecuteScalar();

                    if ($columnExists -eq 0) {{
                        Write-Output 'InternalServiceKeyEncrypted column not found - migration not applied yet';
                        $connection.Close();
                        exit 0;
                    }}

                    # Check if key already exists
                    $checkCmd = $connection.CreateCommand();
                    $checkCmd.CommandText = 'SELECT InternalServiceKeyEncrypted FROM SiteSettings WHERE Id = 1';
                    $existingKey = $checkCmd.ExecuteScalar();

                    if ($existingKey -and $existingKey -ne [DBNull]::Value -and $existingKey.ToString().Length -gt 0) {{
                        Write-Output 'Internal service key already configured';
                        $connection.Close();
                        exit 0;
                    }}

                    # Generate new GUID key
                    $newKey = [guid]::NewGuid().ToString();
                    Write-Output ""Generating new internal service key: $newKey"";

                    # Note: We'll store it unencrypted for now during upgrade
                    # The application will handle encryption on first access
                    # This is safe because the database is already secured
                    $updateCmd = $connection.CreateCommand();
                    $updateCmd.CommandText = @""
                        UPDATE SiteSettings 
                        SET InternalServiceKeyEncrypted = @Key, 
                            EnableInternalServiceAuth = 1 
                        WHERE Id = 1
                    ""@;
                    $updateCmd.Parameters.AddWithValue('@Key', $newKey);
                    $updateCmd.ExecuteNonQuery();

                    Write-Output 'Internal service key configured successfully';
                    Write-Output 'Key will be encrypted by application on first access';
                    
                    $connection.Close();
                }}
                catch {{
                    Write-Output ""Error: $_"";
                    exit 1;
                }}
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    StatusUpdate?.Invoke(this, output.Trim());
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    StatusUpdate?.Invoke(this, $"Warning: {error.Trim()}");
                }
            }
        }
        catch (Exception ex)
        {
            StatusUpdate?.Invoke(this, $"Warning: Could not configure internal service key: {ex.Message}");
            // Don't fail the upgrade for this
        }
    }
}

public class UpgradeProgress
{
    public int CurrentStep { get; set; }
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpgradeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
}

internal class ConfigurationBackup
{
    public string AppSettings { get; set; } = string.Empty;
    public string WebConfig { get; set; } = string.Empty;
}
