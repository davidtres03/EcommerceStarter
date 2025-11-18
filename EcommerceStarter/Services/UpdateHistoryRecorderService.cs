using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Models.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Background service that checks for pending upgrade records in registry
    /// and saves them to UpdateHistory table on application startup
    /// </summary>
    public class UpdateHistoryRecorderService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateHistoryRecorderService> _logger;

        public UpdateHistoryRecorderService(
            IServiceProvider serviceProvider,
            ILogger<UpdateHistoryRecorderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UpdateHistoryRecorder] Service starting - checking for pending upgrade records...");

            try
            {
                await CheckAndRecordPendingUpgradesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateHistoryRecorder] Error checking for pending upgrades");
            }

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UpdateHistoryRecorder] Service stopping");
            return Task.CompletedTask;
        }

        private async Task CheckAndRecordPendingUpgradesAsync()
        {
            try
            {
                // Look for any EcommerceStarter sites with pending update history
                using var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EcommerceStarter");
                if (baseKey == null)
                {
                    _logger.LogInformation("[UpdateHistoryRecorder] No EcommerceStarter registry key found");
                    return;
                }

                foreach (var siteName in baseKey.GetSubKeyNames())
                {
                    var pendingPath = $@"SOFTWARE\EcommerceStarter\{siteName}\PendingUpdateHistory";
                    using var pendingKey = Registry.LocalMachine.OpenSubKey(pendingPath);

                    if (pendingKey != null)
                    {
                        _logger.LogInformation("[UpdateHistoryRecorder] Found pending upgrade for site: {SiteName}", siteName);

                        var version = pendingKey.GetValue("Version") as string;
                        var completedAtStr = pendingKey.GetValue("CompletedAt") as string;
                        var status = pendingKey.GetValue("Status") as string;

                        if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(completedAtStr))
                        {
                            if (DateTime.TryParse(completedAtStr, out var completedAt))
                            {
                                await SaveToUpdateHistoryAsync(version, completedAt, status ?? "Success");

                                // Delete the pending record after successful save
                                Registry.LocalMachine.DeleteSubKeyTree(pendingPath, throwOnMissingSubKey: false);
                                _logger.LogInformation("[UpdateHistoryRecorder] Recorded upgrade to v{Version} and deleted pending record", version);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateHistoryRecorder] Error processing pending upgrades");
            }
        }

        private async Task SaveToUpdateHistoryAsync(string version, DateTime completedAt, string status)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Check if this version is already recorded (avoid duplicates)
                var exists = await dbContext.UpdateHistories
                    .AnyAsync(u => u.Version == version && u.AppliedAt >= completedAt.AddMinutes(-5));

                if (!exists)
                {
                    // Extract changelog for this version
                    var releaseNotes = ExtractChangelogForVersion(version);

                    var updateHistory = new UpdateHistory
                    {
                        Version = version,
                        AppliedAt = completedAt,
                        Status = status,
                        ReleaseNotes = releaseNotes,
                        ApplyDurationSeconds = 0 // Unknown from post-upgrade recording
                    };

                    dbContext.UpdateHistories.Add(updateHistory);
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation("[UpdateHistoryRecorder] Saved upgrade record for v{Version}", version);
                }
                else
                {
                    _logger.LogInformation("[UpdateHistoryRecorder] Upgrade record for v{Version} already exists", version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateHistoryRecorder] Error saving update history for v{Version}", version);
                throw;
            }
        }

        private string ExtractChangelogForVersion(string version)
        {
            try
            {
                // Look for CHANGELOG.md in the application directory
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var changelogPath = Path.Combine(appDirectory, "CHANGELOG.md");

                if (!File.Exists(changelogPath))
                {
                    _logger.LogWarning("[UpdateHistoryRecorder] CHANGELOG.md not found at {Path}", changelogPath);
                    return $"Automatically upgraded to version {version}";
                }

                var changelogContent = File.ReadAllText(changelogPath);

                // Extract the section for this version
                // Pattern: ## [version] - date ... content until next ##
                var versionPattern = $@"##\s*\[{Regex.Escape(version)}\].*?\n(.*?)(?=\n##\s|\z)";
                var match = Regex.Match(changelogContent, versionPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var versionChangelog = match.Groups[1].Value.Trim();
                    
                    // Clean up the changelog (remove excessive whitespace, limit length)
                    versionChangelog = Regex.Replace(versionChangelog, @"\n{3,}", "\n\n");
                    
                    // Limit to reasonable length for database storage (2000 chars)
                    if (versionChangelog.Length > 2000)
                    {
                        versionChangelog = versionChangelog.Substring(0, 1997) + "...";
                    }

                    return versionChangelog;
                }
                else
                {
                    _logger.LogWarning("[UpdateHistoryRecorder] No changelog entry found for version {Version}", version);
                    return $"Automatically upgraded to version {version}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateHistoryRecorder] Error extracting changelog for version {Version}", version);
                return $"Automatically upgraded to version {version}";
            }
        }
    }
}
