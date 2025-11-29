using System;

namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Represents progress of a download operation
/// Compatible with IProgress<T> for async reporting
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// Bytes received so far
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// Total bytes to download
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Percentage complete (0-100)
    /// </summary>
    public int PercentComplete => TotalBytes > 0
        ? (int)((BytesReceived * 100) / TotalBytes)
        : 0;

    /// <summary>
    /// Download speed in MB/s
    /// </summary>
    public double SpeedMBps { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan ETA { get; set; }

    /// <summary>
    /// Elapsed time since download started
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Human-readable size
    /// </summary>
    public string FormattedTotalSize => FormatBytes(TotalBytes);

    /// <summary>
    /// Human-readable downloaded size
    /// </summary>
    public string FormattedReceivedSize => FormatBytes(BytesReceived);

    /// <summary>
    /// Format bytes as human-readable string
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Human-readable progress string
    /// </summary>
    public override string ToString()
    {
        if (TotalBytes == 0)
            return "Starting download...";

        return $"{PercentComplete}% - {FormattedReceivedSize} / {FormattedTotalSize} " +
               $"({SpeedMBps:0.00} MB/s, ETA: {ETA.TotalSeconds:0}s)";
    }
}
