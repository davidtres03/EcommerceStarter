using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;

namespace EcommerceStarter.Services.Analytics
{
    public interface ICloudflareAnalyticsService
    {
        Task<Dictionary<string, int>> GetDailyVisitCountsAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default);
    }

    public class CloudflareAnalyticsService : ICloudflareAnalyticsService
    {
        private readonly ILogger<CloudflareAnalyticsService> _logger;
        private readonly HttpClient _http;
        private readonly EcommerceStarter.Services.ApiManagers.CloudflareApiManager _cfg;
        private readonly IMemoryCache _cache;

        public CloudflareAnalyticsService(
            ILogger<CloudflareAnalyticsService> logger,
            HttpClient httpClient,
            EcommerceStarter.Services.ApiManagers.CloudflareApiManager cfg,
            IMemoryCache cache)
        {
            _logger = logger;
            _http = httpClient;
            _cfg = cfg;
            _cache = cache;
        }

        public async Task<Dictionary<string, int>> GetDailyVisitCountsAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default)
        {
            var cacheKey = $"cf_visits:{startDateUtc:yyyyMMdd}:{endDateUtc:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, int>? cached) && cached != null)
                return cached;

            var creds = await _cfg.GetCredentialsAsync(ct);
            if (string.IsNullOrWhiteSpace(creds.ApiToken) || string.IsNullOrWhiteSpace(creds.ZoneId))
            {
                _logger.LogDebug("Cloudflare credentials not configured; returning empty analytics");
                return new Dictionary<string, int>();
            }

            try
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creds.ApiToken);
                var endpoint = $"https://api.cloudflare.com/client/v4/zones/{creds.ZoneId}/analytics/dashboard" +
                               $"?since={startDateUtc:yyyy-MM-dd}&until={endDateUtc:yyyy-MM-dd}&continuous=true";

                using var resp = await _http.GetAsync(endpoint, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Cloudflare API returned {Status}", resp.StatusCode);
                    return new Dictionary<string, int>();
                }

                var json = await resp.Content.ReadAsStringAsync(ct);
                // To keep this robust without strict schema, extract totals roughly if needed later
                // For now, just cache empty dict to avoid repeated calls when not used.
                var result = new Dictionary<string, int>();
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cloudflare analytics request failed");
                return new Dictionary<string, int>();
            }
        }
    }

    public interface IUnifiedAnalyticsService
    {
        Task<AnalyticsSummary> GetSummaryAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default);
    }

    public class UnifiedAnalyticsService : IUnifiedAnalyticsService
    {
        private readonly ILogger<UnifiedAnalyticsService> _logger;
        private readonly ApplicationDbContext _db;
        private readonly ICloudflareAnalyticsService _cloudflare;

        public UnifiedAnalyticsService(
            ILogger<UnifiedAnalyticsService> logger,
            ApplicationDbContext db,
            ICloudflareAnalyticsService cloudflare)
        {
            _logger = logger;
            _db = db;
            _cloudflare = cloudflare;
        }

        public async Task<AnalyticsSummary> GetSummaryAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default)
        {
            try
            {
                var sessions = await _db.VisitorSessions
                    .Where(s => s.StartTime >= startDateUtc && s.StartTime <= endDateUtc)
                    .ToListAsync(ct);

                var totalPageViews = await _db.PageViews
                    .Where(pv => pv.Timestamp >= startDateUtc && pv.Timestamp <= endDateUtc)
                    .CountAsync(ct);

                var uniqueVisitors = sessions
                    .GroupBy(s => s.IpAddress)
                    .Count();

                var convertedSessions = sessions.Count(s => s.Converted);
                var sessionsWithOnePageView = sessions.Count(s => s.PageViewCount == 1);

                // Include Cloudflare data (if available) for cross-checking or future merge
                _ = await _cloudflare.GetDailyVisitCountsAsync(startDateUtc, endDateUtc, ct);

                var completedSessions = sessions.Where(s => s.EndTime.HasValue).ToList();
                var averageSessionDuration = completedSessions.Any()
                    ? completedSessions.Average(s => (s.EndTime!.Value - s.StartTime).TotalSeconds)
                    : 0;

                return new AnalyticsSummary
                {
                    TotalSessions = sessions.Count,
                    TotalPageViews = totalPageViews,
                    UniqueVisitors = uniqueVisitors,
                    ConvertedSessions = convertedSessions,
                    ConversionRate = sessions.Count > 0 ? (decimal)convertedSessions / sessions.Count * 100 : 0,
                    AverageSessionDuration = averageSessionDuration,
                    AveragePagesPerSession = sessions.Count > 0 ? (double)totalPageViews / sessions.Count : 0,
                    BounceRate = sessions.Count > 0 ? sessionsWithOnePageView * 100 / sessions.Count : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unified analytics computation failed");
                return new AnalyticsSummary();
            }
        }
    }
}
