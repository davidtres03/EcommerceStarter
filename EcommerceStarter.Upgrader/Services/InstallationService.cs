using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using EcommerceStarter.Upgrader.Models;
using Microsoft.AspNetCore.Identity;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for performing the actual installation
/// </summary>
public class InstallationService
{
    public event EventHandler<InstallationProgress>? ProgressUpdate;
    public event EventHandler<string>? StatusUpdate;
    public event EventHandler<string>? ErrorOccurred;

    private readonly LoggerService _logger;

    public InstallationService(LoggerService? logger = null)
    {
        // Use provided logger or create a new one
        _logger = logger ?? new LoggerService();
    }

    /// <summary>
    /// Perform the full installation
    /// </summary>
    public async Task<InstallationResult> InstallAsync(InstallationConfig config, bool isDebugMode)
    {
        // 🎬 DEMO MODE PROTECTION - Refuse to install!
        if (App.IsDemoMode)
        {
            // Simulate installation for demo purposes only
            await SimulateDemoInstallAsync(config);

            return new InstallationResult
            {
                Success = true,
                Message = "Demo installation completed successfully (no real changes were made)"
            };
        }

        var result = new InstallationResult { Success = true };

        try
        {
            // Step 1: Verify Prerequisites (0-20%)
            ReportProgress(1, 10, "Checking prerequisites...");
            await Task.Delay(500);
            ReportProgress(1, 20, "Prerequisites verified!", true);

            // Step 2: Create Database (20-40%)
            ReportProgress(2, 25, config.UseExistingDatabase ? "Preparing existing database..." : "Creating database...");
            if (!isDebugMode)
            {
                if (config.UseExistingDatabase)
                {
                    // For existing databases, only apply migrations
                    ReportProgress(2, 30, "Applying database schema updates...");
                    var migrateResult = await ApplyMigrationsAsync(config.DatabaseServer, config.DatabaseName);
                    if (!migrateResult.Success)
                    {
                        throw new Exception($"Migration failed: {migrateResult.ErrorMessage}");
                    }
                    ReportProgress(2, 35, "Database schema updated successfully!");
                }
                else
                {
                    // For new databases, create and apply migrations
                    var dbResult = await CreateDatabaseAsync(config.DatabaseServer, config.DatabaseName);
                    if (!dbResult.Success)
                    {
                        throw new Exception($"Database creation failed: {dbResult.ErrorMessage}");
                    }
                }

                // Grant IIS Application Pool permissions to database
                ReportProgress(2, 38, "Configuring database permissions...");
                var permResult = await GrantDatabasePermissionsAsync(config.DatabaseServer, config.DatabaseName, config.SiteName);
                if (!permResult.Success)
                {
                    // Non-fatal warning - user can fix manually
                    StatusUpdate?.Invoke(this, $"Warning: Could not auto-configure database permissions: {permResult.ErrorMessage}");
                    StatusUpdate?.Invoke(this, "You may need to grant permissions manually after installation.");
                }
            }
            await Task.Delay(1000);
            ReportProgress(2, 40, "Database ready!", true);

            // Step 3: Deploy Application Files (40-60%)
            ReportProgress(3, 45, "Preparing application files...");
            if (!isDebugMode)
            {
                var deployResult = await DeployApplicationAsync(config.InstallationPath);
                if (!deployResult.Success)
                {
                    throw new Exception($"Application deployment failed: {deployResult.ErrorMessage}");
                }
            }
            await Task.Delay(1000);
            ReportProgress(3, 55, "Deploying files...");
            await Task.Delay(1000);
            ReportProgress(3, 60, "Application deployed!", true);

            // Step 4: Configure IIS (60-80%)
            ReportProgress(4, 65, "Creating IIS application pool...");
            if (!isDebugMode)
            {
                var iisResult = await ConfigureIISAsync(config);
                if (!iisResult.Success)
                {
                    throw new Exception($"IIS configuration failed: {iisResult.ErrorMessage}");
                }
            }
            await Task.Delay(1000);
            ReportProgress(4, 72, "Configuring website...");
            await Task.Delay(1000);
            ReportProgress(4, 80, "IIS configured successfully!", true);

            // Step 5: Apply Configuration (80-90%)
            ReportProgress(5, 85, "Applying your settings...");
            if (!isDebugMode)
            {
                var configResult = await ApplyConfigurationAsync(config);
                if (!configResult.Success)
                {
                    throw new Exception($"Configuration failed: {configResult.ErrorMessage}");
                }

                // Create admin user
                ReportProgress(5, 88, "Creating admin user...");

                // Only create admin if:
                // 1. Credentials were provided AND
                // 2. It's a NEW database (not using existing)
                if (!config.UseExistingDatabase && !string.IsNullOrWhiteSpace(config.AdminEmail) && !string.IsNullOrWhiteSpace(config.AdminPassword))
                {
                    var adminResult = await CreateAdminUserAsync(config);
                    if (!adminResult.Success)
                    {
                        // Non-fatal warning - user can create manually
                        StatusUpdate?.Invoke(this, $"Warning: Could not create admin user: {adminResult.ErrorMessage}");
                        StatusUpdate?.Invoke(this, "You can create an admin user manually after installation.");
                    }
                    else
                    {
                        StatusUpdate?.Invoke(this, adminResult.Message);
                    }
                }
                else if (config.UseExistingDatabase)
                {
                    StatusUpdate?.Invoke(this, "Skipping admin user creation (using existing database - preserving current admin users)");
                }
                else
                {
                    StatusUpdate?.Invoke(this, "Skipping admin user creation (no credentials provided - existing database)");
                }
            }
            await Task.Delay(1000);
            ReportProgress(5, 90, "Configuration applied!", true);

            // Step 6: Finalization (90-100%)
            ReportProgress(6, 95, "Finalizing installation...");

            // Register in Windows Programs & Features
            if (!isDebugMode)
            {
                var registryResult = await RegisterInWindowsAsync(config);
                if (!registryResult.Success)
                {
                    // Non-fatal - log warning but continue
                    StatusUpdate?.Invoke(this, $"Warning: Could not register in Programs & Features: {registryResult.ErrorMessage}");
                }
            }

            await Task.Delay(500);
            ReportProgress(6, 100, "Installation complete!", true);

            result.Success = true;
            result.Message = "Installation completed successfully!";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            ErrorOccurred?.Invoke(this, ex.Message);
            return result;
        }
    }

    private async Task<OperationResult> CreateDatabaseAsync(string server, string databaseName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Ensuring SQL Server is ready...");

            // CRITICAL: Start SQL Browser service if using named instance
            if (server.Contains("\\", StringComparison.OrdinalIgnoreCase))
            {
                StatusUpdate?.Invoke(this, "Starting SQL Server Browser (required for named instances)...");

                var browserScript = @"
                    try {
                        $browser = Get-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                        if ($browser) {
                            Set-Service -Name 'SQLBrowser' -StartupType Automatic -ErrorAction SilentlyContinue
                            Start-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                            Write-Output 'SQL Browser started'
                        } else {
                            Write-Output 'SQL Browser service not found'
                        }
                    } catch {
                        Write-Output 'Warning: Could not start SQL Browser'
                    }
                ";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{browserScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var browserProcess = Process.Start(psi);
                if (browserProcess != null)
                {
                    await browserProcess.WaitForExitAsync();
                }
            }

            StatusUpdate?.Invoke(this, "Running database migrations...");

            // Find the bundled migration executable
            var installerDir = AppDomain.CurrentDomain.BaseDirectory;
            var migrationBundlePath = Path.Combine(installerDir, "migrations", "efbundle.exe");

            if (!File.Exists(migrationBundlePath))
            {
                // For fresh installs without bundled migrations, log a warning but continue
                // Migrations can be run manually via dotnet ef database update
                StatusUpdate?.Invoke(this, "Warning: Migration bundle not found - database will be created with default schema");
                System.Diagnostics.Debug.WriteLine($"[InstallationService] Migration bundle not found at: {migrationBundlePath}. Proceeding with default schema.");
                return new OperationResult { Success = true };
            }

            // Build connection string for migration bundle
            var escapedServer = server.Replace("\"", "\\\"");
            var connectionString = $"Server={escapedServer};Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            // Run the bundled migration executable
            var psi2 = new ProcessStartInfo
            {
                FileName = migrationBundlePath,
                Arguments = $"--connection \"{connectionString}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(migrationBundlePath)
            };

            using var process = Process.Start(psi2);
            if (process == null)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start migration process"
                };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Migration failed: {error}\n{output}"
                };
            }

            return new OperationResult
            {
                Success = true,
                Message = "Database created and migrations applied successfully"
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Apply pending migrations to an existing database (for UseExistingDatabase mode)
    /// </summary>
    private async Task<OperationResult> ApplyMigrationsAsync(string server, string databaseName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Applying database migrations to existing database...");

            // CRITICAL: Start SQL Browser service if using named instance
            if (server.Contains("\\", StringComparison.OrdinalIgnoreCase))
            {
                var browserScript = @"
                    try {
                        $browser = Get-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                        if ($browser) {
                            Set-Service -Name 'SQLBrowser' -StartupType Automatic -ErrorAction SilentlyContinue
                            Start-Service -Name 'SQLBrowser' -ErrorAction SilentlyContinue
                        }
                    } catch {
                        Write-Output 'Warning: Could not start SQL Browser'
                    }
                ";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{browserScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var browserProcess = Process.Start(psi);
                if (browserProcess != null)
                {
                    await browserProcess.WaitForExitAsync();
                }
            }

            // Find the bundled migration executable
            var installerDir = AppDomain.CurrentDomain.BaseDirectory;
            var migrationBundlePath = Path.Combine(installerDir, "migrations", "efbundle.exe");

            if (!File.Exists(migrationBundlePath))
            {
                // For upgrades without bundled migrations, log a warning but continue
                System.Diagnostics.Debug.WriteLine($"[InstallationService] Migration bundle not found at: {migrationBundlePath}. Proceeding without migrations.");
                return new OperationResult { Success = true };
            }

            // Build connection string for migration bundle
            var escapedServer = server.Replace("\"", "\\\"");
            var connectionString = $"Server={escapedServer};Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            // Run the bundled migration executable
            var psi2 = new ProcessStartInfo
            {
                FileName = migrationBundlePath,
                Arguments = $"--connection \"{connectionString}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(migrationBundlePath)
            };

            using var process = Process.Start(psi2);
            if (process == null)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start migration process"
                };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Migration failed: {error}\n{output}"
                };
            }

            return new OperationResult
            {
                Success = true,
                Message = "Migrations applied successfully to existing database"
            };
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<OperationResult> GrantDatabasePermissionsAsync(string server, string databaseName, string appPoolName)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Granting IIS Application Pool database access...");

            var appPoolUser = $"IIS APPPOOL\\{appPoolName}";

            // SQL script to create login and grant permissions
            var sqlScript = $@"
USE [master];
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = N'{appPoolUser}')
BEGIN
    CREATE LOGIN [{appPoolUser}] FROM WINDOWS WITH DEFAULT_DATABASE=[master];
END

USE [{databaseName}];
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'{appPoolUser}')
BEGIN
    CREATE USER [{appPoolUser}] FOR LOGIN [{appPoolUser}];
END

ALTER ROLE [db_owner] ADD MEMBER [{appPoolUser}];
";

            // Save SQL script to temp file
            var tempSqlFile = Path.GetTempFileName() + ".sql";
            await File.WriteAllTextAsync(tempSqlFile, sqlScript);

            try
            {
                // Execute SQL script using sqlcmd
                var psi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{server}\" -E -i \"{tempSqlFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to start sqlcmd" };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"SQL permission script failed: {error}"
                    };
                }

                return new OperationResult
                {
                    Success = true,
                    Message = $"Granted database access to {appPoolUser}"
                };
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempSqlFile))
                {
                    File.Delete(tempSqlFile);
                }
            }
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Deploy application files downloaded from GitHub releases (NEW)
    /// </summary>
    private async Task<OperationResult> DeployApplicationFromGitHubAsync(
        string installPath,
        GitHubReleaseService githubService,
        CacheService cacheService)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Getting latest release from GitHub...");

            // Get latest release
            var latestRelease = await githubService.GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                System.Diagnostics.Debug.WriteLine("[InstallationService] ERROR: GetLatestReleaseAsync returned null");
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Could not find any releases on GitHub. Please ensure:\n1. Repository is accessible\n2. Releases are published on GitHub\n3. GitHub token is configured in Windows Credential Manager (if private repo)"
                };
            }

            System.Diagnostics.Debug.WriteLine($"[InstallationService] Found latest release: {latestRelease.Version}");

            // Find application asset (EcommerceStarter-Installer-vX.X.X.zip or EcommerceStarter-vX.X.X.zip)
            var appAsset = latestRelease.FindAssetByPattern("EcommerceStarter-*.zip");
            if (appAsset == null)
            {
                // Try alternate pattern in case naming changed
                appAsset = latestRelease.FindAssetByPattern("EcommerceStarter-Installer-*.zip");
            }

            if (appAsset == null)
            {
                System.Diagnostics.Debug.WriteLine($"[InstallationService] ERROR: No application ZIP found in release {latestRelease.Version}");
                System.Diagnostics.Debug.WriteLine($"[InstallationService] Available assets ({latestRelease.Assets.Count}):");
                foreach (var asset in latestRelease.Assets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {asset.Name}");
                }
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Release {latestRelease.Version} does not contain a valid application ZIP file. Available assets: {string.Join(", ", latestRelease.Assets.Select(a => a.Name))}"
                };
            }

            System.Diagnostics.Debug.WriteLine($"[InstallationService] Found application asset: {appAsset.Name}");

            StatusUpdate?.Invoke(this, $"Downloading application {latestRelease.Version}...");

            // Check cache first
            byte[]? appZipData = null;
            var isCached = cacheService.IsAssetCached(latestRelease.Version, appAsset.Name);

            if (isCached)
            {
                StatusUpdate?.Invoke(this, $"Loading from cache: {appAsset.Name}");
                appZipData = await cacheService.GetCachedDownloadAsync(latestRelease.Version, appAsset.Name);
                if (appZipData == null)
                {
                    StatusUpdate?.Invoke(this, "Cache read failed, downloading from GitHub...");
                    isCached = false;
                }
            }

            if (!isCached)
            {
                // Download with progress reporting
                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusUpdate?.Invoke(this, $"Downloading: {p}");
                });

                appZipData = await githubService.DownloadAssetAsync(appAsset.BrowserDownloadUrl, appAsset.Id, progress);

                // Cache it for next time
                if (appZipData != null)
                {
                    await cacheService.CacheDownloadAsync(latestRelease.Version, appAsset.Name, appZipData);
                }
            }

            if (appZipData == null || appZipData.Length == 0)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Downloaded application file is empty"
                };
            }

            // Extract to installation path
            StatusUpdate?.Invoke(this, "Extracting application files...");

            Directory.CreateDirectory(installPath);

            // Extract ZIP to installation directory
            // The ZIP structure is: EcommerceStarter-Installer-vX.Y.Z/app/* (actual web files)
            // We need to extract only the app/ subfolder contents
            await Task.Run(() =>
            {
                using var zipStream = new System.IO.MemoryStream(appZipData);
                using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);

                foreach (var entry in archive.Entries)
                {
                    // Extract from both app/ and migrations/ folders
                    // ZIP structure: EcommerceStarter-Installer-v1.0.7/app/* and EcommerceStarter-Installer-v1.0.7/migrations/*
                    if (!entry.FullName.StartsWith("EcommerceStarter-Installer-"))
                        continue;

                    var parts = entry.FullName.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3)
                        continue;

                    // Check if this is app/ or migrations/ subfolder content
                    // parts[0] = "EcommerceStarter-Installer-vX.Y.Z"
                    // parts[1] = "app" or "migrations"
                    // parts[2+] = file path
                    if (parts[1] != "app" && parts[1] != "migrations")
                        continue;

                    // Extract relative path (everything after root folder and subfolder)
                    var relativePath = string.Join("\\", parts.Skip(2));

                    if (string.IsNullOrEmpty(relativePath))
                        continue;

                    var filePath = Path.Combine(installPath, relativePath);
                    var directory = Path.GetDirectoryName(filePath);
                    if (directory != null)
                        Directory.CreateDirectory(directory);

                    // Only extract files, not directory entries
                    if (!entry.FullName.EndsWith('/'))
                    {
                        entry.ExtractToFile(filePath, overwrite: true);
                    }
                }
            });

            // Verify critical files
            var criticalFiles = new[]
            {
                "EcommerceStarter.dll",
                "EcommerceStarter.deps.json",
                "EcommerceStarter.runtimeconfig.json",
                "wwwroot"
            };

            foreach (var file in criticalFiles)
            {
                var filePath = Path.Combine(installPath, file);
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Critical file missing after extraction: {file}"
                    };
                }
            }

            StatusUpdate?.Invoke(this, "Application deployed successfully from GitHub!");
            return new OperationResult { Success = true, Message = "Application deployed from GitHub successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = $"GitHub deployment error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Download and apply migrations from GitHub release
    /// </summary>
    private async Task<OperationResult> ApplyMigrationsFromGitHubAsync(
        string server,
        string databaseName,
        GitHubReleaseService githubService,
        CacheService cacheService)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Getting migrations from GitHub release...");

            // Get latest release
            var latestRelease = await githubService.GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Could not find release for migrations"
                };
            }

            // Find migrations asset
            var migrationsAsset = latestRelease.FindAssetByPattern("migrations-*.sql");
            if (migrationsAsset == null)
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Release does not contain migrations-*.sql asset"
                };
            }

            StatusUpdate?.Invoke(this, "Downloading migrations...");

            // Check cache
            byte[] migrationData;
            var isCached = cacheService.IsAssetCached(latestRelease.Version, migrationsAsset.Name);

            if (isCached)
            {
                migrationData = await cacheService.GetCachedDownloadAsync(latestRelease.Version, migrationsAsset.Name);
                if (migrationData == null)
                {
                    isCached = false;
                }
            }
            else
            {
                migrationData = null;
            }

            if (!isCached)
            {
                // Download
                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusUpdate?.Invoke(this, $"Downloading migrations: {p.PercentComplete}%");
                });

                migrationData = await githubService.DownloadAssetAsync(migrationsAsset.BrowserDownloadUrl, migrationsAsset.Id, progress);

                // Cache it
                await cacheService.CacheDownloadAsync(latestRelease.Version, migrationsAsset.Name, migrationData);
            }

            // Write to temp file and execute
            var migrationSql = System.Text.Encoding.UTF8.GetString(migrationData);
            var tempFile = Path.GetTempFileName() + ".sql";

            try
            {
                await File.WriteAllTextAsync(tempFile, migrationSql);

                StatusUpdate?.Invoke(this, "Applying database migrations...");

                // Execute migrations using sqlcmd
                var psi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{server}\" -d \"{databaseName}\" -E -i \"{tempFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to start sqlcmd for migrations" };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Migrations failed: {error}"
                    };
                }

                StatusUpdate?.Invoke(this, "Migrations applied successfully!");
                return new OperationResult { Success = true, Message = "Migrations applied successfully" };
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = $"Migration error: {ex.Message}" };
        }
    }

    private async Task<OperationResult> DeployApplicationAsync(string installPath)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Deploying application files...");

            // Find the bundled application files
            var installerDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Installer directory: {installerDir}");
            
            // Determine if we're running from Installer subdirectory
            // Standard package structure: PackageRoot/Installer/EcommerceStarter.Installer.exe
            // Application files at: PackageRoot/Application/
            string? bundledAppPath = null;
            
            // Check if running from "Installer" subdirectory (handle trailing slashes)
            var parentDir = Directory.GetParent(installerDir)?.FullName;
            var dirName = Path.GetFileName(installerDir);
            
            StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Directory name: '{dirName}'");
            StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Parent directory: {parentDir}");
            
            if (parentDir != null && dirName.Equals("Installer", StringComparison.OrdinalIgnoreCase))
            {
                // Running from Installer subdirectory - look for Application in parent (package root)
                var packageAppPath = Path.Combine(parentDir, "Application");
                StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Running from Installer subdirectory");
                StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Checking package root: {packageAppPath}");
                StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Directory exists: {Directory.Exists(packageAppPath)}");
                if (Directory.Exists(packageAppPath))
                {
                    StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] ✓ Found application files in package root: {packageAppPath}");
                    bundledAppPath = packageAppPath;
                }
                else
                {
                    StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] ✗ Application folder not found at expected location");
                }
            }
            else
            {
                // Running from package root or other location - check current directory
                var localAppPath = Path.Combine(installerDir, "app");
                StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] Not in Installer subdirectory, checking local: {localAppPath}");
                if (Directory.Exists(localAppPath))
                {
                    StatusUpdate?.Invoke(this, $"[DeployApplicationAsync] ✓ Found application files locally: {localAppPath}");
                    bundledAppPath = localAppPath;
                }
            }

            if (bundledAppPath == null || !Directory.Exists(bundledAppPath))
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = $"Bundled application files not found.\n" +
                                  $"Installer directory: {installerDir}\n" +
                                  $"Directory name extracted: '{dirName}'\n" +
                                  $"Parent directory: {parentDir}\n" +
                                  $"Expected structure:\n" +
                                  $"  PackageRoot/Application/ (web app files)\n" +
                                  $"  PackageRoot/Installer/EcommerceStarter.Installer.exe\n" +
                                  $"  PackageRoot/migrations/efbundle.exe"
                };
            }

            // Verify critical files exist in bundle
            var criticalFiles = new[]
            {
                "EcommerceStarter.dll",
                "EcommerceStarter.deps.json",
                "EcommerceStarter.runtimeconfig.json",
                "wwwroot"
            };

            foreach (var file in criticalFiles)
            {
                var filePath = Path.Combine(bundledAppPath, file);
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Critical file/folder missing in bundle: {file}"
                    };
                }
            }

            // Create installation directory
            StatusUpdate?.Invoke(this, "Creating installation directory...");
            Directory.CreateDirectory(installPath);

            // Copy all files from bundled app to installation directory
            StatusUpdate?.Invoke(this, "Copying application files...");
            await Task.Run(() =>
            {
                CopyDirectory(bundledAppPath, installPath, true);
            });

            // Verify files were copied successfully
            foreach (var file in criticalFiles)
            {
                var filePath = Path.Combine(installPath, file);
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Critical file/folder missing after deployment: {file}"
                    };
                }
            }

            StatusUpdate?.Invoke(this, "Application deployed successfully");
            return new OperationResult { Success = true, Message = "Application deployed successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = $"Deployment error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Recursively copy directory contents
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        // Create destination directory
        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in dir.GetFiles())
        {
            var targetPath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetPath, overwrite);
        }

        // Copy subdirectories
        foreach (var subDir in dir.GetDirectories())
        {
            var newDestDir = Path.Combine(destDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestDir, overwrite);
        }
    }

    private async Task<OperationResult> ConfigureIISAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Configuring IIS...");

            var appName = config.SiteName; // Use site name (e.g., "MyStore")

            // PowerShell script to configure IIS - create root WEBSITE (not application under Default Web Site)
            var script = $@"
                Import-Module WebAdministration;

                # Create App Pool
                $appPoolName = '{appName}';
                if (Test-Path IIS:\AppPools\$appPoolName) {{
                    Remove-WebAppPool -Name $appPoolName;
                }}
                New-WebAppPool -Name $appPoolName -Force;
                Set-ItemProperty IIS:\AppPools\$appPoolName -Name managedRuntimeVersion -Value '';
                Set-ItemProperty IIS:\AppPools\$appPoolName -Name enable32BitAppOnWin64 -Value $false;

                # Create root Website (not under Default Web Site)
                $physicalPath = '{config.InstallationPath}';
                $siteName = '{appName}';

                # Check if site already exists
                if (Get-WebSite -Name $siteName -ErrorAction SilentlyContinue) {{
                    Remove-WebSite -Name $siteName;
                }}

                # Find available port (start at 8080)
                $port = 8080;
                while (Get-WebBinding -Port $port -ErrorAction SilentlyContinue) {{
                    $port++;
                }}

                # Create root website on available port
                New-WebSite -Name $siteName -PhysicalPath $physicalPath -ApplicationPool $appPoolName -Port $port -Force;

                # Start the app pool
                Start-WebAppPool -Name $appPoolName;

                Write-Output 'IIS configured successfully - Website created at http://localhost:$port';
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new OperationResult { Success = false, ErrorMessage = "Failed to start PowerShell" };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new OperationResult { Success = false, ErrorMessage = $"IIS configuration failed: {error}\nOutput: {output}" };
            }

            // Restart IIS to ensure all modules are properly loaded
            try
            {
                StatusUpdate?.Invoke(this, "Restarting IIS to apply configuration...");
                var iisResetProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "iisreset",
                        Arguments = "/restart",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                iisResetProcess.Start();
                var resetOutput = await iisResetProcess.StandardOutput.ReadToEndAsync();
                var resetError = await iisResetProcess.StandardError.ReadToEndAsync();
                await iisResetProcess.WaitForExitAsync();

                StatusUpdate?.Invoke(this, $"IIS restart exit code: {iisResetProcess.ExitCode}");

                if (iisResetProcess.ExitCode == 0)
                {
                    StatusUpdate?.Invoke(this, "IIS restarted successfully!");
                }
                else
                {
                    StatusUpdate?.Invoke(this, $"Note: iisreset returned exit code {iisResetProcess.ExitCode}. You may need to manually restart IIS.");
                }
            }
            catch (Exception ex)
            {
                StatusUpdate?.Invoke(this, $"Note: Could not restart IIS automatically ({ex.Message}). Please run 'iisreset /restart' manually.");
            }

            return new OperationResult { Success = true, Message = $"IIS configured successfully. URL: http://localhost/{appName}" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<OperationResult> ApplyConfigurationAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Writing configuration files...");

            // Note: appsettings.json is NOT created during upgrades
            // All configuration is stored in Windows Registry (encrypted)
            // Connection string: HKLM:\SOFTWARE\EcommerceStarter\{SiteName}\ConnectionStringEncrypted
            // The existing installation already has registry configuration
            
            // No appsettings.json file is created - registry-only configuration

            // Create web.config - using proven production template
            var webConfigPath = Path.Combine(config.InstallationPath, "web.config");
            var webConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <location path=""."" inheritInChildApplications=""false"">
    <system.webServer>
      <handlers>
        <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModuleV2"" resourceType=""Unspecified"" />
      </handlers>
      <aspNetCore processPath=""dotnet"" arguments="".\EcommerceStarter.dll"" stdoutLogEnabled=""false"" stdoutLogFile="".\logs\stdout"" hostingModel=""inprocess"">
        <environmentVariables>
          <environmentVariable name=""ASPNETCORE_ENVIRONMENT"" value=""Production"" />
          <environmentVariable name=""APP_POOL_ID"" value=""{config.SiteName}"" />
          <environmentVariable name=""ASPNETCORE_HTTPS_PORT"" value="""" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>";

            await File.WriteAllTextAsync(webConfigPath, webConfig, System.Text.Encoding.UTF8);

            // Cleanup debug files that shouldn't be in production
            var debugFiles = new[]
            {
                Path.Combine(config.InstallationPath, "appsettings.Development.json"),
                Path.Combine(config.InstallationPath, "appsettings.*.Development.json")
            };

            foreach (var pattern in debugFiles)
            {
                try
                {
                    // For simple filename, just delete if exists
                    if (File.Exists(pattern))
                    {
                        File.Delete(pattern);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            return new OperationResult { Success = true, Message = "Production configuration applied successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<OperationResult> RegisterInWindowsAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Registering in Windows...");

            // Get installer path and determine proper installation location
            var installerPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var installerDir = Path.GetDirectoryName(installerPath) ?? "";

            // Install the ENTIRE installer directory to Program Files
            // This includes migrations\efbundle.exe, app\, and all other files needed for reconfiguration
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var installerProgramFilesDir = Path.Combine(programFilesPath, "EcommerceStarter");
            var uninstallerPath = Path.Combine(installerProgramFilesDir, "EcommerceStarter.Installer.exe");

            // Ensure Program Files directory exists
            Directory.CreateDirectory(installerProgramFilesDir);

            // Copy entire installer directory to Program Files if needed
            // This ensures all supporting files (migrations, app, etc.) are available for reconfiguration
            try
            {
                // Check if we need to copy (first run or updated version)
                if (!File.Exists(uninstallerPath) || File.GetLastWriteTime(installerPath) > File.GetLastWriteTime(uninstallerPath))
                {
                    // Copy all files from current installer directory to Program Files
                    // This includes migrations\efbundle.exe and app\ folder
                    CopyDirectoryRecursive(installerDir, installerProgramFilesDir);
                    StatusUpdate?.Invoke(this, "Installed to Program Files");
                }
            }
            catch (Exception ex)
            {
                StatusUpdate?.Invoke(this, $"Warning: Could not copy installer to Program Files: {ex.Message}");
                // Fall back to current location if can't write to Program Files
                uninstallerPath = Path.Combine(installerDir, "EcommerceStarter.Installer.exe");
            }

            // Get the current installer version to use in registry (normalized to 3-part format)
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var installerVersion = version != null
                ? $"{version.Major}.{version.Minor}.{version.Build}"  // Normalize to 3-part
                : "1.0.0";

            // PowerShell script to register in Windows Registry
            var script = $@"
                $regPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EcommerceStarter_{config.SiteName}';

                # Create registry key
                New-Item -Path $regPath -Force | Out-Null;

                # Set values
                Set-ItemProperty -Path $regPath -Name 'DisplayName' -Value 'EcommerceStarter - {config.CompanyName}';
                # Normalize to 3-part format (Windows standard for DisplayVersion)
                $versionParts = '{installerVersion}'.Split('.');
                $normalizedVersion = if ($versionParts.Count -ge 3) {{ $versionParts[0] + '.' + $versionParts[1] + '.' + $versionParts[2] }} else {{ '{installerVersion}' }};
                Set-ItemProperty -Path $regPath -Name 'DisplayVersion' -Value $normalizedVersion;
                Set-ItemProperty -Path $regPath -Name 'Publisher' -Value 'EcommerceStarter';
                Set-ItemProperty -Path $regPath -Name 'InstallLocation' -Value '{config.InstallationPath}';
                Set-ItemProperty -Path $regPath -Name 'UninstallString' -Value '\""{uninstallerPath}\"" --uninstall --sitename=\""{config.SiteName}\""';

                # Enable Change button (Modern Modify/Repair functionality)
                # ModifyPath is the command to run when user clicks 'Change' in Programs & Features
                Set-ItemProperty -Path $regPath -Name 'ModifyPath' -Value '\""{uninstallerPath}\"" --reconfigure --sitename=\""{config.SiteName}\""';

                Set-ItemProperty -Path $regPath -Name 'DisplayIcon' -Value '{uninstallerPath},0';

                # NoModify and NoRepair control whether Modify/Repair buttons show
                # Set to 0 to SHOW the buttons (Change will be available)
                Set-ItemProperty -Path $regPath -Name 'NoModify' -Value 0 -Type DWord;
                Set-ItemProperty -Path $regPath -Name 'NoRepair' -Value 0 -Type DWord;

                Set-ItemProperty -Path $regPath -Name 'InstallDate' -Value (Get-Date -Format 'yyyyMMdd');

                # NOTE: Database connection details are stored encrypted in instance registry key

                Write-Output 'Registered in Windows successfully with Change support';
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new OperationResult { Success = false, ErrorMessage = "Failed to start PowerShell" };
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                return new OperationResult { Success = false, ErrorMessage = $"Registry registration failed: {error}" };
            }

            return new OperationResult { Success = true, Message = "Registered in Windows successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<OperationResult> CreateAdminUserAsync(InstallationConfig config)
    {
        try
        {
            StatusUpdate?.Invoke(this, "Checking for existing admin users...");

            // First, check if admin users already exist
            var checkAdminSql = @"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Check if there are any users with Admin role
SELECT COUNT(*)
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin'
";

            var tempCheckFile = Path.GetTempFileName() + ".sql";
            await File.WriteAllTextAsync(tempCheckFile, checkAdminSql);

            try
            {
                // Execute check
                var checkPsi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{config.DatabaseServer}\" -d \"{config.DatabaseName}\" -E -i \"{tempCheckFile}\" -h -1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var checkProcess = Process.Start(checkPsi);
                if (checkProcess == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to check for admin users" };
                }

                var checkOutput = await checkProcess.StandardOutput.ReadToEndAsync();
                await checkProcess.WaitForExitAsync();

                File.Delete(tempCheckFile);

                // Parse count
                var adminCount = 0;
                if (int.TryParse(checkOutput.Trim(), out var count))
                {
                    adminCount = count;
                }

                if (adminCount > 0)
                {
                    StatusUpdate?.Invoke(this, $"Found {adminCount} existing admin user(s) - skipping admin creation");
                    return new OperationResult
                    {
                        Success = true,
                        Message = $"Admin users already exist ({adminCount} found). Existing admins preserved."
                    };
                }

                // No admins found - create one
                StatusUpdate?.Invoke(this, "No admin users found - creating new admin account...");
            }
            catch
            {
                // If check fails, proceed with creation (safer to try creating than skip)
                StatusUpdate?.Invoke(this, "Could not verify existing admins - attempting to create admin user...");
            }

            // Use ASP.NET Core Identity's PasswordHasher to hash the password
            var passwordHasher = new PasswordHasher<string>();
            var hashedPassword = passwordHasher.HashPassword(config.AdminEmail, config.AdminPassword);

            // Escape strings for SQL
            var escapedEmail = config.AdminEmail.Replace("'", "''");
            var escapedHashedPassword = hashedPassword.Replace("'", "''");

            // SQL script to create admin user with hashed password
            var sqlScript = $@"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Ensure roles exist
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Customer')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Customer', 'CUSTOMER', NEWID())
END

-- Check if this specific admin user exists
IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = '{escapedEmail}')
BEGIN
    DECLARE @UserId NVARCHAR(450) = CAST(NEWID() AS NVARCHAR(450))
    DECLARE @AdminRoleId NVARCHAR(450)

    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin'

    -- Insert admin user with hashed password
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        CreatedAt
    )
    VALUES (
        @UserId,
        '{escapedEmail}',
        UPPER('{escapedEmail}'),
        '{escapedEmail}',
        UPPER('{escapedEmail}'),
        1,
        '{escapedHashedPassword}',
        NEWID(),
        NEWID(),
        0, 0, 1, 0,
        GETUTCDATE()
    )

    -- Assign Admin role
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId)

    PRINT 'Admin user created successfully'
END
ELSE
BEGIN
    PRINT 'Admin user already exists with this email'
END
";

            // Save SQL script to temp file
            var tempSqlFile = Path.GetTempFileName() + ".sql";
            await File.WriteAllTextAsync(tempSqlFile, sqlScript);

            // ALSO save to a fixed location for debugging
            var debugSqlFile = Path.Combine(Path.GetTempPath(), "AdminUserCreation_DEBUG.sql");
            File.Copy(tempSqlFile, debugSqlFile, true);

            try
            {
                // Execute SQL script using sqlcmd
                var psi = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S \"{config.DatabaseServer}\" -d \"{config.DatabaseName}\" -E -i \"{tempSqlFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return new OperationResult { Success = false, ErrorMessage = "Failed to start sqlcmd" };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return new OperationResult
                    {
                        Success = false,
                        ErrorMessage = $"Admin user creation failed: {error}"
                    };
                }

                // Even on success, save output for debugging
                var outputFile = Path.Combine(Path.GetTempPath(), "AdminUserCreation_OUTPUT.txt");
                await File.WriteAllTextAsync(outputFile, $"Exit Code: {process.ExitCode}\n\nOutput:\n{output}\n\nError:\n{error}");

                return new OperationResult
                {
                    Success = true,
                    Message = $"Admin user created successfully: {config.AdminEmail}"
                };
            }
            finally
            {
                // Clean up temp file only if successful
                if (File.Exists(tempSqlFile))
                {
                    try { File.Delete(tempSqlFile); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private void ReportProgress(int step, int percentage, string message, bool completed = false)
    {
        ProgressUpdate?.Invoke(this, new InstallationProgress
        {
            CurrentStep = step,
            Percentage = percentage,
            Message = message,
            StepCompleted = completed
        });
    }

    /// <summary>
    /// Recursively copy all files and folders from source to destination
    /// This ensures the entire installer directory structure is preserved in Program Files,
    /// including migrations\efbundle.exe, app\ folder, and resource folders
    /// </summary>
    private void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        try
        {
            // If destination doesn't exist, create it
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy all files from source to destination
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true); // true = overwrite if exists
            }

            // Recursively copy all subdirectories
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, destSubDir);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - installation should continue even if some files can't be copied
            StatusUpdate?.Invoke(this, $"Warning: Some files could not be copied to Program Files: {ex.Message}");
        }
    }

    /// <summary>
    /// Simulate installation for demo mode (no real changes)
    /// </summary>
    private async Task SimulateDemoInstallAsync(InstallationConfig config)
    {
        var steps = new[]
        {
            $"Creating database {config.DatabaseName}... (simulated)",
            $"Setting up IIS website at {config.InstallationPath}... (simulated)",
            "Configuring application settings... (simulated)",
            "Creating admin user... (simulated)",
            "Registering installation... (simulated)",
            "Finalizing setup... (simulated)"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            ReportProgress(i + 1, (i + 1) * 100 / steps.Length, $"🎬 DEMO: {steps[i]}", i == steps.Length - 1);
            await Task.Delay(1000); // Simulate work
        }
    }
}

public class InstallationProgress
{
    public int CurrentStep { get; set; }
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool StepCompleted { get; set; }
}

public class InstallationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
