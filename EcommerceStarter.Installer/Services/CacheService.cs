using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for caching downloaded packages locally
/// Avoids re-downloading same version multiple times
/// </summary>
public class CacheService
{
    private readonly string _cacheRoot;

    public CacheService()
    {
        // Use LocalAppData for cache location
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheRoot = Path.Combine(appDataPath, "EcommerceStarter", "Cache");

        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheRoot);
    }

    /// <summary>
    /// Get the cache directory path
    /// </summary>
    public string GetCachePath() => _cacheRoot;

    /// <summary>
    /// Get cache path for a specific version
    /// </summary>
    private string GetVersionCachePath(string version)
        => Path.Combine(_cacheRoot, NormalizeVersion(version));

    /// <summary>
    /// Check if a specific version is cached
    /// </summary>
    public bool IsCached(string version)
    {
        try
        {
            var versionPath = GetVersionCachePath(version);
            return Directory.Exists(versionPath) &&
                   Directory.GetFiles(versionPath).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a specific asset is cached for a version
    /// </summary>
    public bool IsAssetCached(string version, string assetName)
    {
        try
        {
            var assetPath = Path.Combine(GetVersionCachePath(version), assetName);
            return File.Exists(assetPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Cache downloaded data
    /// </summary>
    public async Task CacheDownloadAsync(string version, string assetName, byte[] data)
    {
        try
        {
            var versionPath = GetVersionCachePath(version);
            Directory.CreateDirectory(versionPath);

            var assetPath = Path.Combine(versionPath, assetName);
            await File.WriteAllBytesAsync(assetPath, data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to cache download: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieve cached download
    /// </summary>
    public async Task<byte[]?> GetCachedDownloadAsync(string version, string assetName)
    {
        try
        {
            var assetPath = Path.Combine(GetVersionCachePath(version), assetName);

            if (!File.Exists(assetPath))
                return null;

            return await File.ReadAllBytesAsync(assetPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading cache: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get size of cached version (in bytes)
    /// </summary>
    public long GetCachedSize(string version)
    {
        try
        {
            var versionPath = GetVersionCachePath(version);

            if (!Directory.Exists(versionPath))
                return 0;

            long size = 0;
            foreach (var file in Directory.GetFiles(versionPath))
            {
                size += new FileInfo(file).Length;
            }

            return size;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Delete cached version
    /// </summary>
    public void DeleteCache(string version)
    {
        try
        {
            var versionPath = GetVersionCachePath(version);

            if (Directory.Exists(versionPath))
            {
                Directory.Delete(versionPath, true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete all cached data
    /// </summary>
    public void ClearAllCache()
    {
        try
        {
            if (Directory.Exists(_cacheRoot))
            {
                Directory.Delete(_cacheRoot, true);
            }

            // Recreate empty cache directory
            Directory.CreateDirectory(_cacheRoot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up old cached versions (keeps recent N versions)
    /// </summary>
    public void CleanupOldVersions(int keepCount = 5)
    {
        try
        {
            var versionDirs = Directory.GetDirectories(_cacheRoot);

            if (versionDirs.Length <= keepCount)
                return;

            // Sort by modification time, keep newest
            var sorted = versionDirs
                .Select(d => new { Path = d, Time = Directory.GetLastAccessTime(d) })
                .OrderByDescending(x => x.Time)
                .Skip(keepCount);

            foreach (var dir in sorted)
            {
                try
                {
                    Directory.Delete(dir.Path, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not delete {dir.Path}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cleaning cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the cached ZIP file path for a version (if it exists)
    /// </summary>
    public string? GetCachedZipPath(string version)
    {
        try
        {
            var versionPath = GetVersionCachePath(version);

            if (!Directory.Exists(versionPath))
                return null;

            // Look for any .zip file in the cache
            var zipFiles = Directory.GetFiles(versionPath, "*.zip");

            if (zipFiles.Length > 0)
            {
                return zipFiles[0]; // Return first ZIP found
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting cached ZIP: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get total cache size
    /// </summary>
    public long GetTotalCacheSize()
    {
        try
        {
            long size = 0;

            if (!Directory.Exists(_cacheRoot))
                return 0;

            foreach (var dir in Directory.GetDirectories(_cacheRoot))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    size += new FileInfo(file).Length;
                }
            }

            return size;
        }
        catch
        {
            return 0;
        }
    }

    // --- Private Helpers ---

    private static string NormalizeVersion(string version)
    {
        // Remove 'v' prefix if present, use as directory name
        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? version[1..]
            : version;
    }
}
