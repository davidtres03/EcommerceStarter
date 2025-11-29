using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for detecting and analyzing existing EcommerceStarter installations
/// </summary>
public class UpgradeDetectionService
{
    /// <summary>
    /// Detect if there's an existing installation (returns first found for backward compatibility)
    /// </summary>
    public async Task<ExistingInstallation?> DetectExistingInstallationAsync()
    {
        var allInstallations = await DetectAllInstallationsAsync();
        return allInstallations.FirstOrDefault();
    }

    /// <summary>
    /// Detect ALL existing installations
    /// </summary>
    public async Task<List<ExistingInstallation>> DetectAllInstallationsAsync()
    {
        var installations = new List<ExistingInstallation>();

        try
        {
            System.Diagnostics.Debug.WriteLine("[UpgradeDetectionService] === DETECTION START (ALL INSTANCES) ===");

            // Check registry for existing installations
            System.Diagnostics.Debug.WriteLine("[UpgradeDetectionService] Checking registry...");
            var registryInstallations = GetRegistryInstallations();
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Found {registryInstallations.Count} installations in registry");

            if (registryInstallations.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[UpgradeDetectionService] No existing installations found");
                return installations; // Empty list
            }

            // Analyze each installation
            foreach (var installInfo in registryInstallations)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Analyzing installation: {installInfo.SiteName}");

                    var analysis = await AnalyzeInstallationAsync(installInfo);

                    installations.Add(new ExistingInstallation
                    {
                        SiteName = installInfo.SiteName,
                        InstallPath = installInfo.InstallPath,
                        Version = installInfo.Version,
                        InstallDate = installInfo.InstallDate,
                        // Prefer registry values (authoritative), fallback to analysis from appsettings.json
                        DatabaseServer = !string.IsNullOrEmpty(installInfo.DatabaseServer) ? installInfo.DatabaseServer : analysis.DatabaseServer,
                        DatabaseName = !string.IsNullOrEmpty(installInfo.DatabaseName) ? installInfo.DatabaseName : analysis.DatabaseName,
                        HasDatabase = analysis.HasDatabase,
                        ProductCount = analysis.ProductCount,
                        OrderCount = analysis.OrderCount,
                        UserCount = analysis.UserCount,
                        CompanyName = !string.IsNullOrEmpty(installInfo.CompanyName) ? installInfo.CompanyName : analysis.CompanyName,
                        WebAppUrl = installInfo.WebAppUrl,
                        LocalhostPort = installInfo.LocalhostPort,
                        IsHealthy = analysis.IsHealthy,
                        Issues = analysis.Issues
                    });

                    System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] ✓ Successfully analyzed: {installInfo.SiteName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] ✗ Failed to analyze {installInfo.SiteName}: {ex.Message}");

                    // Add installation with minimal info even if analysis fails (better than crashing)
                    installations.Add(new ExistingInstallation
                    {
                        SiteName = installInfo.SiteName,
                        InstallPath = installInfo.InstallPath,
                        Version = installInfo.Version,
                        InstallDate = installInfo.InstallDate,
                        DatabaseServer = installInfo.DatabaseServer,
                        DatabaseName = installInfo.DatabaseName,
                        HasDatabase = false,
                        ProductCount = 0,
                        OrderCount = 0,
                        UserCount = 0,
                        CompanyName = !string.IsNullOrEmpty(installInfo.CompanyName) ? installInfo.CompanyName : "Unknown",
                        WebAppUrl = installInfo.WebAppUrl,
                        LocalhostPort = installInfo.LocalhostPort,
                        IsHealthy = false,
                        Issues = new List<string> { $"Analysis failed: {ex.Message}" }
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] === DETECTION COMPLETE: {installations.Count} valid installations ===");
            return installations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[UpgradeDetectionService] === DETECTION FAILED WITH EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Exception Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Stack: {ex.StackTrace}");

            // Return empty list instead of crashing installer
            return new List<ExistingInstallation>();
        }
    }

    /// <summary>
    /// Get all installations from registry
    /// </summary>
    private List<RegistryInstallInfo> GetRegistryInstallations()
    {
        var installations = new List<RegistryInstallInfo>();

        try
        {
            // Helper to read uninstall entries for a given registry view
            List<RegistryInstallInfo> ReadUninstallForView(RegistryView view)
            {
                var results = new List<RegistryInstallInfo>();
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstallKey == null) return results;

                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    if (!subKeyName.StartsWith("EcommerceStarter", StringComparison.OrdinalIgnoreCase)) continue;
                    using var productKey = uninstallKey.OpenSubKey(subKeyName);
                    if (productKey == null) continue;

                    var installLocation = productKey.GetValue("InstallLocation") as string;
                    var version = productKey.GetValue("DisplayVersion") as string;
                    var installDate = productKey.GetValue("InstallDate") as string;
                    var databaseServer = productKey.GetValue("DatabaseServer") as string;
                    var databaseName = productKey.GetValue("DatabaseName") as string;

                    if (string.IsNullOrEmpty(installLocation)) continue;

                    var siteName = ExtractSiteNameFromKey(subKeyName);

                    // Read per-instance configuration (optional)
                    string webAppUrl = string.Empty;
                    int localhostPort = 0;
                    string companyName = string.Empty;
                    try
                    {
                        using var siteKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter\" + siteName);
                        if (siteKey != null)
                        {
                            webAppUrl = siteKey.GetValue("WebAppUrl") as string ?? string.Empty;
                            var portObj = siteKey.GetValue("LocalhostPort");
                            if (portObj is int i) localhostPort = i;
                            else if (portObj is string s && int.TryParse(s, out var p)) localhostPort = p;

                            companyName = siteKey.GetValue("CompanyName") as string ?? string.Empty;

                            // If DatabaseServer/DatabaseName not in Uninstall key, try encrypted connection string (support String or Binary)
                            if (string.IsNullOrEmpty(databaseServer) || string.IsNullOrEmpty(databaseName))
                            {
                                var encObj = siteKey.GetValue("ConnectionStringEncrypted");
                                if (encObj is byte[] encBytes)
                                {
                                    try
                                    {
                                        var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                                            encBytes,
                                            null,
                                            System.Security.Cryptography.DataProtectionScope.LocalMachine);
                                        var connStr = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                                        (databaseServer, databaseName) = TryParseServerDb(connStr, databaseServer, databaseName);
                                    }
                                    catch { }
                                }
                                else if (encObj is string encBase64 && !string.IsNullOrEmpty(encBase64))
                                {
                                    try
                                    {
                                        var bytes = Convert.FromBase64String(encBase64);
                                        var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                                            bytes,
                                            null,
                                            System.Security.Cryptography.DataProtectionScope.LocalMachine);
                                        var connStr = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                                        (databaseServer, databaseName) = TryParseServerDb(connStr, databaseServer, databaseName);
                                    }
                                    catch { }
                                }
                            }

                            // Fallback: read plain text values from instance key if they exist
                            if (string.IsNullOrEmpty(databaseServer))
                                databaseServer = siteKey.GetValue("DatabaseServer") as string ?? string.Empty;
                            if (string.IsNullOrEmpty(databaseName))
                                databaseName = siteKey.GetValue("DatabaseName") as string ?? string.Empty;
                        }
                    }
                    catch { }

                    results.Add(new RegistryInstallInfo
                    {
                        SiteName = siteName,
                        InstallPath = installLocation,
                        Version = version ?? "Unknown",
                        InstallDate = installDate ?? "Unknown",
                        DatabaseServer = databaseServer ?? string.Empty,
                        DatabaseName = databaseName ?? string.Empty,
                        WebAppUrl = webAppUrl,
                        LocalhostPort = localhostPort,
                        CompanyName = companyName
                    });
                }

                return results;
            }

            // Merge results from both 64-bit and 32-bit views (avoid duplicates by InstallPath)
            var list64 = ReadUninstallForView(RegistryView.Registry64);
            var list32 = ReadUninstallForView(RegistryView.Registry32);
            installations.AddRange(list64);
            foreach (var item in list32)
            {
                if (!installations.Any(x => string.Equals(x.InstallPath, item.InstallPath, StringComparison.OrdinalIgnoreCase)))
                {
                    installations.Add(item);
                }
            }

            // Fallback: enumerate per-instance keys directly if Uninstall entries are missing
            try
            {
                using var instanceRoot = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter");
                if (instanceRoot != null)
                {
                    foreach (var instanceName in instanceRoot.GetSubKeyNames())
                    {
                        if (installations.Any(i => i.SiteName.Equals(instanceName, StringComparison.OrdinalIgnoreCase))) continue;
                        using var siteKey = instanceRoot.OpenSubKey(instanceName);
                        if (siteKey == null) continue;

                        var installPath = siteKey.GetValue("InstallPath") as string ?? string.Empty;
                        var version = siteKey.GetValue("Version") as string ?? "Unknown";
                        var webAppUrl = siteKey.GetValue("WebAppUrl") as string ?? string.Empty;
                        var companyName = siteKey.GetValue("CompanyName") as string ?? string.Empty;
                        int localhostPort = 0;
                        var portObj = siteKey.GetValue("LocalhostPort");
                        if (portObj is int pi) localhostPort = pi; else if (portObj is string ps && int.TryParse(ps, out var pv)) localhostPort = pv;

                        installations.Add(new RegistryInstallInfo
                        {
                            SiteName = instanceName,
                            InstallPath = installPath,
                            Version = version,
                            InstallDate = string.Empty,
                            DatabaseServer = string.Empty,
                            DatabaseName = string.Empty,
                            WebAppUrl = webAppUrl,
                            LocalhostPort = localhostPort,
                            CompanyName = companyName
                        });
                    }
                }
            }
            catch { }
        }
        catch
        {
            // Registry access failed - no installations found
        }

        return installations;
    }

    /// <summary>
    /// Analyze an existing installation
    /// </summary>
    private async Task<InstallationAnalysis> AnalyzeInstallationAsync(RegistryInstallInfo installInfo)
    {
        var analysis = new InstallationAnalysis
        {
            IsHealthy = true,
            Issues = new List<string>()
        };

        try
        {
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Analyzing installation: {installInfo.InstallPath}");

            // Check if files exist
            if (!Directory.Exists(installInfo.InstallPath))
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Installation directory not found");
                analysis.IsHealthy = false;
                analysis.Issues.Add("Installation directory not found");
                return analysis;
            }

            // Try to get connection string from multiple sources
            string? connectionString = null;

            // 1. Try registry (newer installations with encrypted connection strings)
            try
            {
                using var siteKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter\" + installInfo.SiteName);
                if (siteKey != null)
                {
                    var encObj = siteKey.GetValue("ConnectionStringEncrypted");
                    if (encObj is byte[] encBytes)
                    {
                        var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                            encBytes,
                            null,
                            System.Security.Cryptography.DataProtectionScope.LocalMachine);
                        connectionString = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                        System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Read encrypted (binary) connection string from registry");
                    }
                    else if (encObj is string encBase64 && !string.IsNullOrEmpty(encBase64))
                    {
                        var bytes = Convert.FromBase64String(encBase64);
                        var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                            bytes,
                            null,
                            System.Security.Cryptography.DataProtectionScope.LocalMachine);
                        connectionString = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                        System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Read encrypted (base64) connection string from registry");
                    }
                }
            }
            catch (Exception regEx)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Registry connection string read failed: {regEx.Message}");
            }

            // 2. Fallback: Try appsettings.json (older installations)
            if (string.IsNullOrEmpty(connectionString))
            {
                var appsettingsPath = Path.Combine(installInfo.InstallPath, "appsettings.json");
                if (File.Exists(appsettingsPath))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(appsettingsPath);
                        connectionString = ExtractConnectionString(json);
                        System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Read connection string from appsettings.json");
                    }
                    catch (Exception jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] appsettings.json read failed: {jsonEx.Message}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    var (server, database) = ParseConnectionString(connectionString);
                    analysis.DatabaseServer = server;
                    analysis.DatabaseName = database;

                    System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Database: {database} @ {server}");

                    // Try to connect to database and get statistics (non-blocking with timeout)
                    var dbStatsTask = GetDatabaseStatisticsAsync(connectionString);
                    if (await Task.WhenAny(dbStatsTask, Task.Delay(3000)) == dbStatsTask)
                    {
                        var dbStats = await dbStatsTask;
                        if (dbStats != null)
                        {
                            analysis.HasDatabase = true;
                            analysis.ProductCount = dbStats.ProductCount;
                            analysis.OrderCount = dbStats.OrderCount;
                            analysis.UserCount = dbStats.UserCount;
                            analysis.CompanyName = dbStats.CompanyName;
                            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Database connection successful");
                        }
                        else
                        {
                            analysis.HasDatabase = false;
                            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Database connection failed");
                        }
                    }
                    else
                    {
                        analysis.HasDatabase = false;
                        System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Database connection timed out");
                    }
                }
                catch (Exception dbEx)
                {
                    analysis.HasDatabase = false;
                    System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Database exception: {dbEx.Message}");
                }
            }
            else
            {
                // No connection string found - not fatal, just means we can't query DB
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] No connection string found in appsettings.json");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Analysis error: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Stack trace: {ex.StackTrace}");
            analysis.IsHealthy = false;
            analysis.Issues.Add($"Analysis error: {ex.Message}");
        }

        return analysis;
    }

    private (string Server, string Database) TryParseServerDb(string connStr, string currentServer, string currentDb)
    {
        string server = currentServer ?? string.Empty;
        string database = currentDb ?? string.Empty;
        try
        {
            var parts = connStr.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;
                var key = kv[0].Trim();
                var val = kv[1].Trim();
                if (key.Equals("Server", StringComparison.OrdinalIgnoreCase) || key.Equals("Data Source", StringComparison.OrdinalIgnoreCase)) server = val;
                if (key.Equals("Database", StringComparison.OrdinalIgnoreCase) || key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase)) database = val;
            }
        }
        catch { }
        return (server, database);
    }

    /// <summary>
    /// Get statistics from the database
    /// </summary>
    private async Task<DatabaseStatistics?> GetDatabaseStatisticsAsync(string connectionString)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Attempting database connection...");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Connection string (first 100 chars): {connectionString.Substring(0, Math.Min(100, connectionString.Length))}");

            using var connection = new SqlConnection(connectionString);

            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Opening connection...");
            await connection.OpenAsync();
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Connection opened successfully");

            var stats = new DatabaseStatistics();

            // Get product count
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Products", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    stats.ProductCount = result != null ? Convert.ToInt32(result) : 0;
                }
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Product count: {stats.ProductCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] ERROR getting product count: {ex.GetType().Name}: {ex.Message}");
                stats.ProductCount = -1; // Indicate error
            }

            // Get order count
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Orders", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    stats.OrderCount = result != null ? Convert.ToInt32(result) : 0;
                }
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Order count: {stats.OrderCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] ERROR getting order count: {ex.GetType().Name}: {ex.Message}");
                stats.OrderCount = -1;
            }

            // Get user count
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    stats.UserCount = result != null ? Convert.ToInt32(result) : 0;
                }
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] User count: {stats.UserCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] ERROR getting user count: {ex.GetType().Name}: {ex.Message}");
                stats.UserCount = -1;
            }

            // Get company name from site settings
            try
            {
                using (var cmd = new SqlCommand("SELECT TOP 1 CompanyName FROM SiteSettings WHERE CompanyName IS NOT NULL AND CompanyName != 'PRODUCTION_DATABASE_INITIALIZED' AND CompanyName != 'DEVELOPMENT_DATABASE_INITIALIZED'", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    stats.CompanyName = result?.ToString() ?? "Unknown";
                }
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Company name: {stats.CompanyName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] ERROR getting company name: {ex.GetType().Name}: {ex.Message}");
                stats.CompanyName = "Unknown";
            }

            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Database analysis complete");
            return stats;
        }
        catch (InvalidOperationException invalidEx)
        {
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] InvalidOperationException during database access: {invalidEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Stack trace: {invalidEx.StackTrace}");
            if (invalidEx.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Inner exception: {invalidEx.InnerException.Message}");
            }
            return null; // Database not accessible
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Exception during database access: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeDetectionService] Stack trace: {ex.StackTrace}");
            return null; // Database not accessible
        }
    }

    /// <summary>
    /// Extract connection string from appsettings.json
    /// </summary>
    private string ExtractConnectionString(string json)
    {
        try
        {
            // Simple JSON parsing - look for DefaultConnection
            var startIndex = json.IndexOf("\"DefaultConnection\"");
            if (startIndex == -1) return string.Empty;

            var colonIndex = json.IndexOf(":", startIndex);
            var valueStart = json.IndexOf("\"", colonIndex) + 1;
            var valueEnd = json.IndexOf("\"", valueStart);

            var connectionString = json.Substring(valueStart, valueEnd - valueStart);

            // Unescape JSON escape sequences (e.g., \\\\ becomes \\, which becomes \)
            connectionString = connectionString.Replace("\\\\", "\\");

            return connectionString;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Parse connection string to extract server and database
    /// </summary>
    private (string Server, string Database) ParseConnectionString(string connectionString)
    {
        var server = "Unknown";
        var database = "Unknown";

        try
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    if (key == "server" || key == "data source")
                        server = value;
                    else if (key == "database" || key == "initial catalog")
                        database = value;
                }
            }
        }
        catch { }

        return (server, database);
    }

    /// <summary>
    /// Extract site name from registry key
    /// </summary>
    private string ExtractSiteNameFromKey(string keyName)
    {
        // Key format: "EcommerceStarter_SiteName" or just "EcommerceStarter"
        var parts = keyName.Split('_');
        return parts.Length > 1 ? parts[1] : "Unknown";
    }

    /// <summary>
    /// Normalize version to 3-part format for registry DisplayVersion (Windows standard)
    /// Handles both 3-part (1.0.9) and 4-part (1.0.9.5) formats, always returns 3-part
    /// </summary>
    private string NormalizeVersionForRegistry(string version)
    {
        if (string.IsNullOrEmpty(version))
            return "1.0.0";

        try
        {
            var parts = version.Split('.');
            if (parts.Length >= 3)
            {
                // Take first 3 parts (Major.Minor.Patch), discard Revision/4th part
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            else if (parts.Length == 2)
            {
                // Ensure at least 3-part format
                return $"{parts[0]}.{parts[1]}.0";
            }
        }
        catch { }

        return version; // Return as-is if parsing fails
    }
}

/// <summary>
/// Information about an existing installation
/// </summary>
public class ExistingInstallation
{
    public string SiteName { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool HasDatabase { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public int UserCount { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string WebAppUrl { get; set; } = string.Empty;
    public int LocalhostPort { get; set; }
    public bool IsHealthy { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Registry installation information
/// </summary>
internal class RegistryInstallInfo
{
    public string SiteName { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string InstallDate { get; set; } = string.Empty;
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string WebAppUrl { get; set; } = string.Empty;
    public int LocalhostPort { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

/// <summary>
/// Installation analysis results
/// </summary>
internal class InstallationAnalysis
{
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool HasDatabase { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public int UserCount { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Database statistics
/// </summary>
internal class DatabaseStatistics
{
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public int UserCount { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
