namespace EcommerceStarter.WindowsService;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EcommerceStarter.WindowsService.Services;

/// <summary>
/// Background service worker for EcommerceStarter
/// Handles: Health monitoring, update checking, auto-restart on failures
/// </summary>
public class BackgroundServiceWorker : BackgroundService
{
    private readonly ILogger<BackgroundServiceWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly UpdateService _updateService;
    private readonly IConfiguration _configuration;
    private readonly RegistryConfigService _registryConfig;
    private DateTime _lastHealthCheck = DateTime.MinValue;
    private DateTime _lastUpdateCheck = DateTime.MinValue;
    private DateTime _lastQueueProcessing = DateTime.MinValue;
    private DateTime _lastStatusLog = DateTime.MinValue;
    private DateTime _serviceStartTime = DateTime.UtcNow;
    private int _consecutiveFailures = 0;
    private int _totalHealthChecks = 0;
    private int _successfulHealthChecks = 0;
    private const int MAX_FAILURES_BEFORE_RESTART = 5;
    private const int HEALTH_CHECK_INTERVAL_MINUTES = 5;
    private const int UPDATE_CHECK_INTERVAL_HOURS = 24;
    private const int QUEUE_PROCESSING_INTERVAL_SECONDS = 30;
    private const int STATUS_LOG_INTERVAL_MINUTES = 5;
    private string ECOMMERCE_STARTER_URL => _registryConfig.GetBaseUrl(_configuration["EcommerceStarterUrl"] ?? "http://localhost:8080");

    public BackgroundServiceWorker(ILogger<BackgroundServiceWorker> logger, HttpClient httpClient, UpdateService updateService, IConfiguration configuration, RegistryConfigService registryConfig)
    {
        _logger = logger;
        _httpClient = httpClient;
        _updateService = updateService;
        _configuration = configuration;
        _registryConfig = registryConfig;
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
                    // Process queued events every 30 seconds
                    if (DateTime.Now - _lastQueueProcessing > TimeSpan.FromSeconds(QUEUE_PROCESSING_INTERVAL_SECONDS))
                    {
                        await ProcessQueuedEventsAsync(stoppingToken);
                        _lastQueueProcessing = DateTime.Now;
                    }

                    // Perform health check every 5 minutes
                    if (DateTime.Now - _lastHealthCheck > TimeSpan.FromMinutes(HEALTH_CHECK_INTERVAL_MINUTES))
                    {
                        await PerformHealthCheckAsync(stoppingToken);
                        _lastHealthCheck = DateTime.Now;
                    }

                    // Log service status every 5 minutes
                    if (DateTime.Now - _lastStatusLog > TimeSpan.FromMinutes(STATUS_LOG_INTERVAL_MINUTES))
                    {
                        await LogServiceStatusAsync(stoppingToken);
                        _lastStatusLog = DateTime.Now;
                    }

                    // Check for updates every 24 hours
                    if (DateTime.Now - _lastUpdateCheck > TimeSpan.FromHours(UPDATE_CHECK_INTERVAL_HOURS))
                    {
                        await CheckForUpdatesAsync(stoppingToken);
                        _lastUpdateCheck = DateTime.Now;
                    }

                    // Wait before next iteration (10 seconds)
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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

    private async Task ProcessQueuedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing queued analytics and audit events");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(25)); // Timeout before next iteration

            var response = await _httpClient.PostAsync($"{ECOMMERCE_STARTER_URL}/api/QueueProcessing/process", null, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                
                // Only log if events were actually processed
                if (content.Contains("\"eventsProcessed\":") && !content.Contains("\"eventsProcessed\":0"))
                {
                    _logger.LogInformation("Queue processing response: {Content}", content);
                }
            }
            else
            {
                _logger.LogWarning("Queue processing returned status: {Status}", response.StatusCode);
            }
        }
        catch (HttpRequestException)
        {
            // Only log warning if this isn't the initial startup (web app might not be ready yet)
            if (_lastQueueProcessing > DateTime.MinValue)
            {
                _logger.LogWarning("Queue processing HTTP request failed - is web app running at {Url}?", ECOMMERCE_STARTER_URL);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Queue processing timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing queued events");
            await LogServiceErrorAsync("Queue Processing", "Error", ex.Message, ex.StackTrace);
        }
    }

    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Performing health check on {url}", ECOMMERCE_STARTER_URL);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var response = await _httpClient.GetAsync($"{ECOMMERCE_STARTER_URL}/", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Health check passed. Status: {status}", response.StatusCode);
                _consecutiveFailures = 0;
                _successfulHealthChecks++;
            }
            else
            {
                _logger.LogWarning("Health check failed with status: {status}", response.StatusCode);
                _consecutiveFailures++;
            }
            
            _totalHealthChecks++;
        }
        catch (HttpRequestException)
        {
            // Only log if this isn't during initial startup
            if (_lastHealthCheck > DateTime.MinValue)
            {
                _logger.LogWarning("Health check HTTP request failed - web app may be restarting");
            }
            _consecutiveFailures++;
            _totalHealthChecks++;
        }
        catch (TaskCanceledException)
        {
            if (_lastHealthCheck > DateTime.MinValue)
            {
                _logger.LogWarning("Health check timed out");
            }
            _consecutiveFailures++;
            _totalHealthChecks++;
        }

        // If too many failures, log an alert
        if (_consecutiveFailures >= MAX_FAILURES_BEFORE_RESTART)
        {
            _logger.LogError("Service health check failed {count} consecutive times. Consider manual intervention.", _consecutiveFailures);
            await LogServiceErrorAsync(
                "Health Check", 
                "Critical", 
                $"Service health check failed {_consecutiveFailures} consecutive times. Web service may be down.");
            // Note: Auto-restart logic would go here in a real production system
            // For now, just log the issue
        }
    }

    /// <summary>
    /// Log current service status to the database via API
    /// </summary>
    private async Task LogServiceStatusAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Get queue size from queue stats API
            int queueSize = 0;
            try
            {
                var queueStatsResponse = await _httpClient.GetAsync($"{ECOMMERCE_STARTER_URL}/api/QueueProcessing/stats", stoppingToken);
                if (queueStatsResponse.IsSuccessStatusCode)
                {
                    var statsJson = await queueStatsResponse.Content.ReadAsStringAsync(stoppingToken);
                    var stats = System.Text.Json.JsonDocument.Parse(statsJson);
                    queueSize = stats.RootElement.GetProperty("queueSize").GetInt32();
                }
            }
            catch { /* Queue size retrieval is optional */ }

            // Calculate uptime percentage
            var uptimePercent = _totalHealthChecks > 0 
                ? (decimal)_successfulHealthChecks / _totalHealthChecks * 100 
                : 100m;

            // Get system metrics
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryUsageMb = process.WorkingSet64 / (1024m * 1024m);

            // Measure web app response time
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isWebOnline = false;
            try
            {
                var pingResponse = await _httpClient.GetAsync($"{ECOMMERCE_STARTER_URL}/", stoppingToken);
                isWebOnline = pingResponse.IsSuccessStatusCode;
            }
            catch { /* Web app offline */ }
            sw.Stop();

            // Build status log
            var statusLog = new
            {
                isWebServiceOnline = isWebOnline,
                responseTimeMs = (int)sw.ElapsedMilliseconds,
                isBackgroundServiceRunning = true,
                pendingOrdersCount = 0, // Placeholder
                memoryUsageMb = (int)Math.Round(memoryUsageMb),
                cpuUsagePercent = 0m, // CPU monitoring requires additional setup
                databaseConnected = true, // Assume true if we can write logs
                activeUserCount = 0, // Would require session tracking
                queueSize = queueSize,
                uptimePercent = Math.Round(uptimePercent, 2),
                errorMessage = _consecutiveFailures >= MAX_FAILURES_BEFORE_RESTART 
                    ? $"Health check failed {_consecutiveFailures} times" 
                    : null
            };

            // Send to web app API
            var json = System.Text.Json.JsonSerializer.Serialize(statusLog);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ECOMMERCE_STARTER_URL}/api/admin/service/status/log", content, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Service status logged successfully");
            }
            else
            {
                _logger.LogWarning("Failed to log service status: {status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging service status");
            await LogServiceErrorAsync("Status Logging", "Error", ex.Message, ex.StackTrace);
        }
    }

    /// <summary>
    /// Log error to ServiceErrorLogs table via API
    /// </summary>
    private async Task LogServiceErrorAsync(string source, string severity, string message, string? stackTrace = null)
    {
        try
        {
            var errorLog = new
            {
                source = source,
                severity = severity,
                message = message,
                stackTrace = stackTrace
            };

            var json = System.Text.Json.JsonSerializer.Serialize(errorLog);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{ECOMMERCE_STARTER_URL}/api/admin/service/errors/log", content);
        }
        catch
        {
            // Silently fail - don't want error logging to cause cascading failures
        }
    }

    /// <summary>
    /// Log update history to UpdateHistories table via API
    /// </summary>
    private async Task LogUpdateHistoryAsync(string version, string status, string? releaseNotes = null, string? errorMessage = null, int durationSeconds = 0)
    {
        try
        {
            var updateLog = new
            {
                version = version,
                appliedAt = DateTime.UtcNow,
                status = status,
                releaseNotes = releaseNotes,
                errorMessage = errorMessage,
                applyDurationSeconds = durationSeconds
            };

            var json = System.Text.Json.JsonSerializer.Serialize(updateLog);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ECOMMERCE_STARTER_URL}/api/admin/service/updates/log", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Update history logged: {version} - {status}", version, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log update history");
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
                await LogServiceErrorAsync("Update Service", "Warning", "Update check failed");
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

                    var applyStartTime = DateTime.UtcNow;
                    // Apply update (this will wait for low-traffic window)
                    bool applySuccess = await _updateService.ApplyUpdateAsync(
                        Path.Combine(Path.GetTempPath(), "EcommerceStarter-Updates", Path.GetFileName(checkResult.DownloadUrl)),
                        cancellationToken);

                    var applyDuration = (int)(DateTime.UtcNow - applyStartTime).TotalSeconds;
                    
                    if (applySuccess)
                    {
                        _logger.LogInformation("Update applied successfully");
                        await LogUpdateHistoryAsync(
                            checkResult.LatestVersion ?? "Unknown",
                            "Success",
                            checkResult.ReleaseNotes,
                            null,
                            applyDuration);
                    }
                    else
                    {
                        _logger.LogError("Failed to apply update");
                        await LogUpdateHistoryAsync(
                            checkResult.LatestVersion ?? "Unknown",
                            "Failed",
                            checkResult.ReleaseNotes,
                            "Update application failed",
                            applyDuration);
                        await LogServiceErrorAsync("Update Service", "Error", "Failed to apply update");
                    }
                }
                else
                {
                    _logger.LogError("Failed to download update");
                    await LogServiceErrorAsync("Update Service", "Error", "Failed to download update from " + checkResult.DownloadUrl);
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
