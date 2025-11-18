using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Registry migration service - handles versioned migrations similar to database migrations
/// </summary>
public class RegistryMigrationService
{
    private readonly LoggerService _logger;

    public RegistryMigrationService(LoggerService? logger = null)
    {
        _logger = logger ?? new LoggerService();
    }

    /// <summary>
    /// Run all pending registry migrations for an installation
    /// </summary>
    public async Task<MigrationResult> RunMigrationsAsync(string siteName)
    {
        var result = new MigrationResult { Success = true };

        try
        {
            _logger.Log($"[RegistryMigration] Checking migrations for: {siteName}");

            // Get current registry schema version
            var currentVersion = await GetRegistrySchemaVersionAsync(siteName);
            _logger.Log($"[RegistryMigration] Current schema version: {currentVersion}");

            // Get all available migrations
            var migrations = GetMigrations();
            var pendingMigrations = new List<IRegistryMigration>();

            foreach (var migration in migrations)
            {
                if (migration.Version > currentVersion)
                {
                    pendingMigrations.Add(migration);
                    _logger.Log($"[RegistryMigration] Pending: v{migration.Version} - {migration.Description}");
                }
            }

            if (pendingMigrations.Count == 0)
            {
                _logger.Log("[RegistryMigration] No pending migrations");
                result.Message = "Registry schema is up to date";
                return result;
            }

            // Run pending migrations in order
            foreach (var migration in pendingMigrations)
            {
                _logger.Log($"[RegistryMigration] Running: v{migration.Version} - {migration.Description}");

                try
                {
                    var migrationSuccess = await migration.ExecuteAsync(siteName);
                    if (!migrationSuccess)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Migration v{migration.Version} failed";
                        result.FailedMigrations.Add(migration.Version);
                        _logger.Log($"[RegistryMigration] ✗ Failed: v{migration.Version}");
                        break; // Stop on first failure
                    }

                    result.AppliedMigrations.Add(migration.Version);
                    _logger.Log($"[RegistryMigration] ✓ Completed: v{migration.Version}");

                    // Update schema version after successful migration
                    await UpdateRegistrySchemaVersionAsync(siteName, migration.Version);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Migration v{migration.Version} threw exception: {ex.Message}";
                    result.FailedMigrations.Add(migration.Version);
                    _logger.Log($"[RegistryMigration] ✗ Exception: v{migration.Version} - {ex.Message}");
                    break;
                }
            }

            if (result.Success)
            {
                result.Message = $"Applied {result.AppliedMigrations.Count} migration(s) successfully";
                _logger.Log($"[RegistryMigration] ✓ All migrations completed");
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.Log($"[RegistryMigration] ✗ Migration system error: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Get current registry schema version for an installation
    /// </summary>
    private async Task<int> GetRegistrySchemaVersionAsync(string siteName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\EcommerceStarter\{siteName}");
                if (key == null)
                {
                    // Registry doesn't exist yet - version 0 (needs initial migration)
                    return 0;
                }

                var version = key.GetValue("RegistrySchemaVersion");
                if (version == null)
                {
                    // Registry exists but no schema version - version 0 (legacy install)
                    return 0;
                }

                return (int)version;
            }
            catch
            {
                return 0;
            }
        });
    }

    /// <summary>
    /// Update registry schema version after successful migration
    /// </summary>
    private async Task UpdateRegistrySchemaVersionAsync(string siteName, int version)
    {
        try
        {
            var escapedSiteName = siteName.Replace("'", "''").Replace("\\", "\\\\");
            var script = $@"
                $configPath = 'HKLM:\SOFTWARE\EcommerceStarter\{escapedSiteName}';
                if (Test-Path $configPath) {{
                    Set-ItemProperty -Path $configPath -Name 'RegistrySchemaVersion' -Value {version} -Type DWord;
                    Set-ItemProperty -Path $configPath -Name 'LastMigrationDate' -Value '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' -Type String;
                    Write-Output 'Schema version updated to {version}';
                }} else {{
                    Write-Error 'Registry path not found';
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
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"[RegistryMigration] Warning: Could not update schema version: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all available migrations in order
    /// </summary>
    private List<IRegistryMigration> GetMigrations()
    {
        return new List<IRegistryMigration>
        {
            new RegistryMigration_v1_InitialSchema(),
            // Future migrations go here:
            // new RegistryMigration_v2_AddNewFeature(),
            // new RegistryMigration_v3_UpdatePaths(),
        };
    }

    /// <summary>
    /// Check if migrations are needed for an installation
    /// </summary>
    public async Task<bool> HasPendingMigrationsAsync(string siteName)
    {
        var currentVersion = await GetRegistrySchemaVersionAsync(siteName);
        var migrations = GetMigrations();

        foreach (var migration in migrations)
        {
            if (migration.Version > currentVersion)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Interface for registry migrations
/// </summary>
public interface IRegistryMigration
{
    int Version { get; }
    string Description { get; }
    Task<bool> ExecuteAsync(string siteName);
}

/// <summary>
/// Migration v1: Initial registry schema
/// Creates the base registry structure with all standard keys
/// </summary>
public class RegistryMigration_v1_InitialSchema : IRegistryMigration
{
    public int Version => 1;
    public string Description => "Initialize registry configuration structure";

    public async Task<bool> ExecuteAsync(string siteName)
    {
        try
        {
            var escapedSiteName = siteName.Replace("'", "''").Replace("\\", "\\\\");

            // Check if registry already has data (upgrade from pre-registry version)
            using var existingKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\EcommerceStarter\{siteName}");
            bool hasExistingData = existingKey != null;

            if (hasExistingData)
            {
                // Registry exists but has no schema version - this is a legacy install
                // Just add the schema version marker, don't overwrite existing data
                var script = $@"
                    $configPath = 'HKLM:\SOFTWARE\EcommerceStarter\{escapedSiteName}';
                    if (Test-Path $configPath) {{
                        Set-ItemProperty -Path $configPath -Name 'RegistrySchemaVersion' -Value 1 -Type DWord -ErrorAction SilentlyContinue;
                        Set-ItemProperty -Path $configPath -Name 'LastMigrationDate' -Value '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' -Type String -ErrorAction SilentlyContinue;
                        Write-Output 'Legacy installation upgraded to schema v1';
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
                    return process.ExitCode == 0;
                }

                return false;
            }
            else
            {
                // New installation - will be created by installer's WriteConfigurationToRegistryAsync
                // Just mark schema version as 1
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration v1 failed: {ex.Message}");
            return false;
        }
    }
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<int> AppliedMigrations { get; set; } = new();
    public List<int> FailedMigrations { get; set; } = new();
}
