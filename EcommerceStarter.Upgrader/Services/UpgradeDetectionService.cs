using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for detecting and analyzing existing EcommerceStarter installations
/// </summary>
public class UpgradeDetectionService
{
    private readonly LoggerService? _logger;

    public UpgradeDetectionService(LoggerService? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect if there's an existing installation
    /// </summary>
    public async Task<ExistingInstallation?> DetectExistingInstallationAsync()
    {
        try
        {
            // Check registry for existing installations
            var registryInstallations = GetRegistryInstallations();

            if (registryInstallations.Count == 0)
            {
                return null; // No existing installation
            }

            // Get the first installation found
            var installInfo = registryInstallations.First();

            // Analyze the installation
            var analysis = await AnalyzeInstallationAsync(installInfo);

            return new ExistingInstallation
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
                CompanyName = analysis.CompanyName,
                IsHealthy = analysis.IsHealthy,
                Issues = analysis.Issues
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Installation detection failed", ex);
            throw; // Re-throw so caller can handle
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
            // Check for EcommerceStarter installations
            using var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey != null)
            {
                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    if (subKeyName.StartsWith("EcommerceStarter", StringComparison.OrdinalIgnoreCase))
                    {
                        using var productKey = uninstallKey.OpenSubKey(subKeyName);
                        if (productKey != null)
                        {
                            var displayName = productKey.GetValue("DisplayName") as string;
                            var installLocation = productKey.GetValue("InstallLocation") as string;
                            var version = productKey.GetValue("DisplayVersion") as string;
                            var installDate = productKey.GetValue("InstallDate") as string;
                            var databaseServer = productKey.GetValue("DatabaseServer") as string;
                            var databaseName = productKey.GetValue("DatabaseName") as string;

                            if (!string.IsNullOrEmpty(installLocation))
                            {
                                installations.Add(new RegistryInstallInfo
                                {
                                    SiteName = ExtractSiteNameFromKey(subKeyName),
                                    InstallPath = installLocation,
                                    Version = version ?? "Unknown",
                                    InstallDate = installDate ?? "Unknown",
                                    DatabaseServer = databaseServer ?? string.Empty,
                                    DatabaseName = databaseName ?? string.Empty
                                });
                            }
                        }
                    }
                }
            }
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
            // Check if files exist
            if (!Directory.Exists(installInfo.InstallPath))
            {
                analysis.IsHealthy = false;
                analysis.Issues.Add("Installation directory not found");
                _logger?.LogWarning($"Installation directory not found: {installInfo.InstallPath}");
                return analysis;
            }

            // Fallback: Read appsettings.json for legacy installations only
            var appsettingsPath = Path.Combine(installInfo.InstallPath, "appsettings.json");

            if (File.Exists(appsettingsPath))
            {
                var json = await File.ReadAllTextAsync(appsettingsPath);
                var connectionString = ExtractConnectionString(json);

                if (!string.IsNullOrEmpty(connectionString))
                {
                    var (server, database) = ParseConnectionString(connectionString);
                    analysis.DatabaseServer = server;
                    analysis.DatabaseName = database;

                    // Try to connect to database and get statistics
                    var dbStats = await GetDatabaseStatisticsAsync(connectionString);
                    if (dbStats != null)
                    {
                        analysis.HasDatabase = true;
                        analysis.ProductCount = dbStats.ProductCount;
                        analysis.OrderCount = dbStats.OrderCount;
                        analysis.UserCount = dbStats.UserCount;
                        analysis.CompanyName = dbStats.CompanyName;
                    }
                    else
                    {
                        analysis.HasDatabase = false;
                        analysis.Issues.Add("Cannot connect to database");
                        _logger?.LogWarning($"Cannot connect to database: {database} @ {server}");
                    }
                }
            }
            else
            {
                analysis.IsHealthy = false;
                analysis.Issues.Add("Configuration file not found");
                _logger?.LogWarning($"Configuration file not found: {appsettingsPath}");
            }
        }
        catch (Exception ex)
        {
            analysis.IsHealthy = false;
            analysis.Issues.Add($"Analysis error: {ex.Message}");
            _logger?.LogError("Installation analysis failed", ex);
        }

        return analysis;
    }

    /// <summary>
    /// Get statistics from the database
    /// </summary>
    private async Task<DatabaseStatistics?> GetDatabaseStatisticsAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var stats = new DatabaseStatistics();

            // Get product count
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Products", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    stats.ProductCount = result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Could not get product count: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Could not get order count: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Could not get user count: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Could not get company name: {ex.Message}");
                stats.CompanyName = "Unknown";
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Database connection failed: {ex.Message}");
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
