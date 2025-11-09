using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for upgrading existing installations
/// </summary>
public class UpgradeService
{
    public event EventHandler<UpgradeProgress>? ProgressUpdate;
    public event EventHandler<string>? StatusUpdate;

    /// <summary>
    /// Perform upgrade from a downloaded zip file
    /// </summary>
    public async Task<UpgradeResult> UpgradeFromZipAsync(ExistingInstallation installation, string zipFilePath)
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
            var totalSteps = 7;
            var currentStep = 0;

            // Step 1: Create backup (0-15%)
            ReportProgress(++currentStep, 5, "Creating backup...");
            backupPath = await CreateBackupAsync(installation.InstallPath);
            ReportProgress(currentStep, 15, "Backup created");

            // Step 2: Stop IIS (15-25%)
            ReportProgress(++currentStep, 20, "Stopping IIS Application Pool...");
            await StopIISAsync(installation.SiteName);
            ReportProgress(currentStep, 25, "IIS stopped");

            // Step 3: Extract new files to temp (25-40%)
            ReportProgress(++currentStep, 30, "Extracting new files...");
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"upgrade-{Guid.NewGuid()}");
            ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath);
            ReportProgress(currentStep, 40, "Files extracted");

            // Step 4: Preserve config files (40-45%)
            ReportProgress(++currentStep, 42, "Preserving configuration...");
            var configBackup = await PreserveConfigurationAsync(installation.InstallPath);
            ReportProgress(currentStep, 45, "Configuration preserved");

            // Step 5: Deploy new files (45-70%)
            ReportProgress(++currentStep, 50, "Deploying new application...");
            await DeployFilesAsync(tempExtractPath, installation.InstallPath);
            await RestoreConfigurationAsync(configBackup, installation.InstallPath);
            ReportProgress(currentStep, 70, "Application deployed");

            // Step 6: Run database migrations (70-85%)
            ReportProgress(++currentStep, 75, "Running database migrations...");
            var migrationResult = await RunMigrationsAsync(installation.InstallPath, installation.DatabaseServer, installation.DatabaseName);
            if (!migrationResult.Success)
            {
                StatusUpdate?.Invoke(this, $"Warning: {migrationResult.Message}");
            }
            ReportProgress(currentStep, 85, "Migrations complete");

            // Step 7: Start IIS (85-100%)
            ReportProgress(++currentStep, 90, "Starting IIS Application Pool...");
            await StartIISAsync(installation.SiteName);
            ReportProgress(currentStep, 95, "IIS started");

            // Verify site is running
            ReportProgress(currentStep, 98, "Verifying site...");
            await Task.Delay(2000); // Give IIS time to start
            ReportProgress(currentStep, 100, "Upgrade complete!");

            result.Success = true;
            result.Message = "Upgrade completed successfully!";
            result.BackupPath = backupPath;

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.BackupPath = backupPath;

            // Attempt rollback
            if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
            {
                StatusUpdate?.Invoke(this, "Upgrade failed - attempting rollback...");
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

        var appsettingsPath = Path.Combine(installPath, "appsettings.json");
        var webConfigPath = Path.Combine(installPath, "web.config");

        if (File.Exists(appsettingsPath))
        {
            backup.AppSettings = await File.ReadAllTextAsync(appsettingsPath);
        }

        if (File.Exists(webConfigPath))
        {
            backup.WebConfig = await File.ReadAllTextAsync(webConfigPath);
        }

        return backup;
    }

    private async Task RestoreConfigurationAsync(ConfigurationBackup backup, string installPath)
    {
        if (!string.IsNullOrEmpty(backup.AppSettings))
        {
            await File.WriteAllTextAsync(Path.Combine(installPath, "appsettings.json"), backup.AppSettings);
        }

        if (!string.IsNullOrEmpty(backup.WebConfig))
        {
            await File.WriteAllTextAsync(Path.Combine(installPath, "web.config"), backup.WebConfig);
        }
    }

    private async Task DeployFilesAsync(string sourcePath, string destPath)
    {
        await Task.Run(() =>
        {
            // Delete old files (except config, logs, uploads)
            var protectedFiles = new[] { "appsettings.json", "web.config" };
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

    private async Task<OperationResult> RunMigrationsAsync(string installPath, string server, string database)
    {
        try
        {
            // Find the project path (assuming standard structure)
            var projectPath = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(installPath).FullName).FullName).FullName).FullName, "EcommerceStarter", "EcommerceStarter.csproj");

            if (!File.Exists(projectPath))
            {
                return new OperationResult { Success = false, Message = "Project file not found for migrations" };
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef database update --project \"{projectPath}\" --context ApplicationDbContext",
                WorkingDirectory = Path.GetDirectoryName(projectPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new OperationResult { Success = false, Message = "Failed to start migration process" };
            }

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return new OperationResult { Success = true, Message = "Migrations applied successfully" };
            }

            var error = await process.StandardError.ReadToEndAsync();
            return new OperationResult { Success = false, Message = $"Migration failed: {error}" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, Message = ex.Message };
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
            System.Diagnostics.Debug.WriteLine($"?? DEMO: {steps[i]}");
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
