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
            new RegistryMigration_v2_MigrateDatabaseToEncrypted(),
            // Future migrations go here:
            // new RegistryMigration_v3_AddNewFeature(),
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
/// MIGRATES data from old Uninstall location to new EcommerceStarter location
/// </summary>
public class RegistryMigration_v1_InitialSchema : IRegistryMigration
{
    public int Version => 1;
    public string Description => "Initialize registry configuration structure and migrate from Uninstall location";

    public async Task<bool> ExecuteAsync(string siteName)
    {
        try
        {
            var escapedSiteName = siteName.Replace("'", "''").Replace("\\", "\\\\");

            // Check both possible old locations
            var oldLocation1 = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{siteName}";
            var oldLocation2 = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter";
            
            // Check if new location already exists
            using var newKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\EcommerceStarter\{siteName}");
            bool newLocationExists = newKey != null;

            if (newLocationExists)
            {
                // New location exists - just mark schema version
                var markScript = $@"
                    $configPath = 'HKLM:\SOFTWARE\EcommerceStarter\{escapedSiteName}';
                    if (Test-Path $configPath) {{
                        Set-ItemProperty -Path $configPath -Name 'RegistrySchemaVersion' -Value 1 -Type DWord -ErrorAction SilentlyContinue;
                        Set-ItemProperty -Path $configPath -Name 'LastMigrationDate' -Value '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' -Type String -ErrorAction SilentlyContinue;
                        Write-Output 'Schema version marked';
                    }}
                ";

                var psi1 = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{markScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process1 = Process.Start(psi1);
                if (process1 != null)
                {
                    await process1.WaitForExitAsync();
                }
                return true;
            }

            // New location doesn't exist - migrate from old location
            var migrationScript = $@"
                $oldPath1 = 'HKLM:\{oldLocation1.Replace(@"\", @"\\")}';
                $oldPath2 = 'HKLM:\{oldLocation2.Replace(@"\", @"\\")}';
                $newPath = 'HKLM:\SOFTWARE\EcommerceStarter\{escapedSiteName}';

                # Find which old location has data
                $sourcePath = $null;
                if (Test-Path $oldPath1) {{
                    $sourcePath = $oldPath1;
                    Write-Output ""Found data at: $oldPath1"";
                }} elseif (Test-Path $oldPath2) {{
                    $sourcePath = $oldPath2;
                    Write-Output ""Found data at: $oldPath2"";
                }} else {{
                    Write-Output ""No old installation data found - clean install"";
                    # Create new location with schema marker
                    New-Item -Path $newPath -Force | Out-Null;
                    Set-ItemProperty -Path $newPath -Name 'RegistrySchemaVersion' -Value 1 -Type DWord;
                    Set-ItemProperty -Path $newPath -Name 'LastMigrationDate' -Value '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' -Type String;
                    exit 0;
                }}

                # Create new registry location
                New-Item -Path $newPath -Force | Out-Null;
                Write-Output ""Created new registry location: $newPath"";

                # Copy ALL properties from old to new location
                $properties = Get-ItemProperty -Path $sourcePath;
                $copiedCount = 0;
                
                foreach ($prop in $properties.PSObject.Properties) {{
                    $name = $prop.Name;
                    # Skip system properties
                    if ($name -match '^PS') {{ continue; }}
                    
                    $value = $properties.$name;
                    try {{
                        # Determine type and copy
                        if ($value -is [int]) {{
                            Set-ItemProperty -Path $newPath -Name $name -Value $value -Type DWord -ErrorAction Stop;
                        }} elseif ($value -is [string]) {{
                            Set-ItemProperty -Path $newPath -Name $name -Value $value -Type String -ErrorAction Stop;
                        }} else {{
                            Set-ItemProperty -Path $newPath -Name $name -Value $value -ErrorAction Stop;
                        }}
                        $copiedCount++;
                        Write-Output ""Copied: $name"";
                    }} catch {{
                        Write-Warning ""Failed to copy: $name - $_"";
                    }}
                }}

                # Add schema version marker
                Set-ItemProperty -Path $newPath -Name 'RegistrySchemaVersion' -Value 1 -Type DWord;
                Set-ItemProperty -Path $newPath -Name 'LastMigrationDate' -Value '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' -Type String;
                
                Write-Output ""Migration complete: Copied $copiedCount properties"";
                exit 0;
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{migrationScript}\"",
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

                Console.WriteLine($"Migration v1 output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Migration v1 warnings: {error}");
                }

                return process.ExitCode == 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration v1 failed: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Migration v2: Migrate plain text database credentials to encrypted connection string
/// Enterprise security: Remove plain text DatabaseServer/DatabaseName from registry
/// </summary>
public class RegistryMigration_v2_MigrateDatabaseToEncrypted : IRegistryMigration
{
    public int Version => 2;
    public string Description => "Migrate database credentials to encrypted connection string (enterprise security)";

    public async Task<bool> ExecuteAsync(string siteName)
    {
        try
        {
            var escapedSiteName = siteName.Replace("'", "''").Replace("\\", "\\\\");
            
            var script = $@"
                $configPath = 'HKLM:\SOFTWARE\EcommerceStarter\{escapedSiteName}';
                
                if (-not (Test-Path $configPath)) {{
                    Write-Output 'Registry key not found';
                    exit 1;
                }}

                $props = Get-ItemProperty -Path $configPath;
                
                # Check if plain text database credentials exist
                $dbServer = $props.DatabaseServer;
                $dbName = $props.DatabaseName;
                $hasEncrypted = $props.PSObject.Properties.Name -contains 'ConnectionStringEncrypted';
                
                if (($dbServer -or $dbName) -and -not $hasEncrypted) {{
                    # Plain text credentials exist but no encrypted connection string
                    # Build connection string
                    $serverEscaped = $dbServer -replace '\\\\', '\\\\\\\\';
                    $connStr = ""Server=$serverEscaped;Database=$dbName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"";
                    
                    # Encrypt using DPAPI
                    $bytes = [System.Text.Encoding]::UTF8.GetBytes($connStr);
                    $encrypted = [System.Security.Cryptography.ProtectedData]::Protect(
                        $bytes,
                        $null,
                        [System.Security.Cryptography.DataProtectionScope]::LocalMachine
                    );
                    $encryptedBase64 = [Convert]::ToBase64String($encrypted);
                    
                    # Write encrypted connection string
                    Set-ItemProperty -Path $configPath -Name 'ConnectionStringEncrypted' -Value $encryptedBase64 -Type String;
                    Write-Output 'Encrypted connection string created';
                }}
                
                # Remove plain text values (whether we migrated them or they already had encrypted)
                if ($props.PSObject.Properties.Name -contains 'DatabaseServer') {{
                    Remove-ItemProperty -Path $configPath -Name 'DatabaseServer' -ErrorAction SilentlyContinue;
                    Write-Output 'Removed plain text DatabaseServer';
                }}
                
                if ($props.PSObject.Properties.Name -contains 'DatabaseName') {{
                    Remove-ItemProperty -Path $configPath -Name 'DatabaseName' -ErrorAction SilentlyContinue;
                    Write-Output 'Removed plain text DatabaseName';
                }}
                
                Write-Output 'Migration v2 completed - enterprise security enforced';
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
                await process.WaitForExitAsync();
                Console.WriteLine($"Migration v2 output: {output}");
                return process.ExitCode == 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration v2 failed: {ex.Message}");
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
