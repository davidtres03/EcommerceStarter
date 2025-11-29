using System;
using System.Collections.Generic;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for handling version comparisons and upgrade path logic
/// </summary>
public class VersionService
{
    public const string CURRENT_VERSION = "1.2.1.0";

    // Define upgrade paths and any special handling needed
    private static readonly Dictionary<string, VersionInfo> KnownVersions = new()
    {
        { "0.9.0", new VersionInfo { Version = "0.9.0", ReleaseDate = new DateTime(2024, 9, 15), Breaking = false } },
        { "0.9.5", new VersionInfo { Version = "0.9.5", ReleaseDate = new DateTime(2024, 10, 1), Breaking = false } },
        { "1.0.0", new VersionInfo { Version = "1.0.0", ReleaseDate = new DateTime(2024, 11, 1), Breaking = true, BreakingChanges = new[] { "Database schema updated", "Configuration format changed" } } },
        { "1.0.1", new VersionInfo { Version = "1.0.1", ReleaseDate = new DateTime(2024, 11, 13), Breaking = false, Notes = "AI Control Panel added, improved upgrade path" } },
    };

    /// <summary>
    /// Compare two versions (sem ver style with 4-part support)
    /// Returns: -1 if v1 < v2, 0 if equal, 1 if v1 > v2
    /// </summary>
    public static int CompareVersions(string version1, string version2)
    {
        if (version1 == "Unknown" || version2 == "Unknown")
        {
            return 0; // Cannot compare unknown versions
        }

        var v1 = ParseVersion(version1);
        var v2 = ParseVersion(version2);

        if (v1 == null || v2 == null)
        {
            return 0; // Cannot parse versions
        }

        if (v1.Major != v2.Major)
            return v1.Major.CompareTo(v2.Major);
        if (v1.Minor != v2.Minor)
            return v1.Minor.CompareTo(v2.Minor);
        if (v1.Patch != v2.Patch)
            return v1.Patch.CompareTo(v2.Patch);
        // Compare Revision (4th digit) for versions like 1.0.9.1 vs 1.0.9.2
        return v1.Revision.CompareTo(v2.Revision);
    }

    /// <summary>
    /// Check if an upgrade is available
    /// </summary>
    public static bool IsUpgradeAvailable(string currentVersion)
    {
        return CompareVersions(currentVersion, CURRENT_VERSION) < 0;
    }

    /// <summary>
    /// Get information about a specific version
    /// </summary>
    public static VersionInfo? GetVersionInfo(string version)
    {
        if (KnownVersions.TryGetValue(version, out var info))
        {
            return info;
        }

        // Try to parse unknown version
        var parsed = ParseVersion(version);
        if (parsed != null)
        {
            return new VersionInfo
            {
                Version = version,
                ReleaseDate = DateTime.MinValue,
                Breaking = false
            };
        }

        return null;
    }

    /// <summary>
    /// Determine which database migrations need to be run
    /// </summary>
    public static List<string> GetRequiredMigrations(string fromVersion, string toVersion)
    {
        var migrations = new List<string>();

        if (fromVersion == "Unknown")
        {
            // Fresh install - no migrations needed
            return migrations;
        }

        var comparison = CompareVersions(fromVersion, toVersion);
        if (comparison >= 0)
        {
            // Already at or past target version
            return migrations;
        }

        // Add migrations based on version jumps
        if (CompareVersions(fromVersion, "1.0.0") < 0 && CompareVersions(toVersion, "1.0.0") >= 0)
        {
            migrations.Add("20241101_MajorDatabaseSchemaUpdate");
            migrations.Add("20241101_AddAITables");
        }

        if (CompareVersions(fromVersion, "1.0.1") < 0 && CompareVersions(toVersion, "1.0.1") >= 0)
        {
            migrations.Add("20241113_AddAITables");
        }

        return migrations;
    }

    /// <summary>
    /// Check if special pre-upgrade steps are needed
    /// </summary>
    public static PreUpgradeRequirements GetPreUpgradeRequirements(string fromVersion)
    {
        var requirements = new PreUpgradeRequirements { CanUpgrade = true };

        if (fromVersion == "Unknown")
        {
            requirements.Message = "Fresh installation detected - no pre-upgrade steps needed";
            return requirements;
        }

        if (CompareVersions(fromVersion, "0.9.0") < 0)
        {
            requirements.CanUpgrade = false;
            requirements.Message = "Cannot upgrade from version older than 0.9.0. Please perform manual migration.";
            return requirements;
        }

        // Check for breaking changes
        var versionInfo = GetVersionInfo(fromVersion);
        if (versionInfo?.Breaking == true)
        {
            requirements.HasBreakingChanges = true;
            requirements.Message = $"Version {fromVersion} contains breaking changes. User confirmation required.";
            requirements.BreakingChanges = versionInfo.BreakingChanges;
        }

        return requirements;
    }

    /// <summary>
    /// Parse a semantic version string
    /// </summary>
    private static SemVersion? ParseVersion(string version)
    {
        if (string.IsNullOrEmpty(version) || version == "Unknown")
        {
            return null;
        }

        // Remove 'v' prefix if present
        var versionStr = version.StartsWith("v") ? version.Substring(1) : version;

        var parts = versionStr.Split('.');
        if (parts.Length < 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return null;
        }

        // Parse Revision (4th digit) if present
        var revision = 0;
        if (parts.Length >= 4 && !int.TryParse(parts[3], out revision))
        {
            revision = 0; // Default to 0 if can't parse
        }

        return new SemVersion { Major = major, Minor = minor, Patch = patch, Revision = revision };
    }

    // Inner types for version info
    public class VersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public bool Breaking { get; set; }
        public string[] BreakingChanges { get; set; } = Array.Empty<string>();
        public string? Notes { get; set; }
    }

    public class PreUpgradeRequirements
    {
        public bool CanUpgrade { get; set; }
        public string? Message { get; set; }
        public bool HasBreakingChanges { get; set; }
        public string[]? BreakingChanges { get; set; }
    }

    private class SemVersion
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int Revision { get; set; } = 0; // 4th digit for versions like 1.0.9.1
    }
}
