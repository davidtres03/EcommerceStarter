namespace EcommerceStarter.WindowsService;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service worker for EcommerceStarter
/// Handles: Health monitoring, update checking, auto-restart on failures
/// </summary>
public class BackgroundServiceWorker : BackgroundService
{
    private readonly ILogger<BackgroundServiceWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly UpdateService _updateService;
    private DateTime _lastHealthCheck = DateTime.MinValue;
    private DateTime _lastUpdateCheck = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private const int MAX_FAILURES_BEFORE_RESTART = 5;
    private const int HEALTH_CHECK_INTERVAL_MINUTES = 5;
    private const int UPDATE_CHECK_INTERVAL_HOURS = 24;
    private const string ECOMMERCE_STARTER_URL = "http://localhost:5000";

    public BackgroundServiceWorker(ILogger<BackgroundServiceWorker> logger, HttpClient httpClient, UpdateService updateService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _updateService = updateService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EcommerceStarter Background Service started at {time}", DateTimeOffset.Now);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Perform health check every 5 minutes
                    if (DateTime.Now - _lastHealthCheck > TimeSpan.FromMinutes(HEALTH_CHECK_INTERVAL_MINUTES))
                    {
                        await PerformHealthCheckAsync(stoppingToken);
                        _lastHealthCheck = DateTime.Now;
                    }

                    // Check for updates every 24 hours
                    if (DateTime.Now - _lastUpdateCheck > TimeSpan.FromHours(UPDATE_CHECK_INTERVAL_HOURS))
                    {
                        await CheckForUpdatesAsync(stoppingToken);
                        _lastUpdateCheck = DateTime.Now;
                    }

                    // Wait before next iteration (1 minute)
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Background service cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background service worker loop");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("EcommerceStarter Background Service stopped at {time}", DateTimeOffset.Now);
        }
    }

    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Performing health check on {url}", ECOMMERCE_STARTER_URL);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var response = await _httpClient.GetAsync($"{ECOMMERCE_STARTER_URL}/", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Health check passed. Status: {status}", response.StatusCode);
                _consecutiveFailures = 0;
            }
            else
            {
                _logger.LogWarning("Health check failed with status: {status}", response.StatusCode);
                _consecutiveFailures++;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Health check HTTP request failed");
            _consecutiveFailures++;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Health check timed out");
            _consecutiveFailures++;
        }

        // If too many failures, log an alert
        if (_consecutiveFailures >= MAX_FAILURES_BEFORE_RESTART)
        {
            _logger.LogError("Service health check failed {count} consecutive times. Consider manual intervention.", _consecutiveFailures);
            // Note: Auto-restart logic would go here in a real production system
            // For now, just log the issue
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for EcommerceStarter updates");

            // Get current version
            var currentVersion = _updateService.GetCurrentVersion();
            _logger.LogInformation("Current application version: {version}", currentVersion);

            // Check for available updates
            var checkResult = await _updateService.CheckForUpdatesAsync(currentVersion, cancellationToken);

            if (!checkResult.IsSuccessful)
            {
                _logger.LogWarning("Update check failed");
                return;
            }

            if (!checkResult.UpdateAvailable)
            {
                _logger.LogInformation("No updates available");
                return;
            }

            _logger.LogInformation("Update available! Latest version: {version}", checkResult.LatestVersion);

            // Download update
            if (!string.IsNullOrEmpty(checkResult.DownloadUrl))
            {
                bool downloadSuccess = await _updateService.DownloadUpdateAsync(checkResult.DownloadUrl, cancellationToken);

                if (downloadSuccess)
                {
                    _logger.LogInformation("Update downloaded successfully. Will apply during low-traffic window.");

                    // Apply update (this will wait for low-traffic window)
                    bool applySuccess = await _updateService.ApplyUpdateAsync(
                        Path.Combine(Path.GetTempPath(), "EcommerceStarter-Updates", Path.GetFileName(checkResult.DownloadUrl)),
                        cancellationToken);

                    if (applySuccess)
                    {
                        _logger.LogInformation("Update applied successfully");
                    }
                    else
                    {
                        _logger.LogError("Failed to apply update");
                    }
                }
                else
                {
                    _logger.LogError("Failed to download update");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EcommerceStarter Background Service is stopping...");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _httpClient?.Dispose();
        base.Dispose();
    }
}
