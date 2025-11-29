using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Middleware
{
    public class SuspiciousActivityDetectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SuspiciousActivityDetectionMiddleware> _logger;
        private readonly Services.ISecurityAuditService _audit;
        private readonly IMemoryCache _cache;

        // Default fallback; actual threshold comes from security settings
        private const int DefaultThreshold = 20; // errors per minute

        public SuspiciousActivityDetectionMiddleware(
            RequestDelegate next,
            ILogger<SuspiciousActivityDetectionMiddleware> logger,
            Services.ISecurityAuditService audit,
            IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _audit = audit;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            var status = context.Response.StatusCode;
            if (status >= 400)
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var nowMinute = DateTime.UtcNow;
                var key = $"suspicious:{ip}:{nowMinute:yyyyMMddHHmm}";
                var count = _cache.GetOrCreate(key, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return 0;
                });
                count++;
                _cache.Set(key, count, TimeSpan.FromMinutes(1));

                // Pull thresholds from security settings
                var settingsService = context.RequestServices.GetService(typeof(EcommerceStarter.Services.ISecuritySettingsService)) as EcommerceStarter.Services.ISecuritySettingsService;
                var settings = settingsService != null ? await settingsService.GetSettingsAsync() : new EcommerceStarter.Models.SecuritySettings();
                var threshold = settings.ErrorSpikeThresholdPerMinute > 0 ? settings.ErrorSpikeThresholdPerMinute : DefaultThreshold;

                // Calculate consecutive minute spikes
                var requiredConsecutive = settings.ErrorSpikeConsecutiveMinutes > 0 ? settings.ErrorSpikeConsecutiveMinutes : 1;
                var consecutiveAtOrAbove = 0;
                for (int i = 0; i < requiredConsecutive; i++)
                {
                    var minute = nowMinute.AddMinutes(-i);
                    var minuteKey = $"suspicious:{ip}:{minute:yyyyMMddHHmm}";
                    var minuteCount = _cache.Get<int?>(minuteKey) ?? 0;
                    if (minuteCount >= threshold)
                    {
                        consecutiveAtOrAbove++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (consecutiveAtOrAbove >= requiredConsecutive)
                {
                    var ua = context.Request.Headers["User-Agent"].ToString();
                    var path = context.Request.Path.ToString();
                    await _audit.LogSecurityEventAsync(
                        eventType: "SuspiciousActivity",
                        severity: "Medium",
                        ipAddress: ip,
                        details: $"High error rate detected. Status={status}, Path={path}, Count={count}/min for {consecutiveAtOrAbove} consecutive minute(s)",
                        endpoint: path,
                        userAgent: ua);
                    _logger.LogWarning("Suspicious activity threshold reached for {IP}", ip);

                    // Auto-block logic: escalate to permanent if enabled, otherwise temporary block
                    if (settings.EnableIpBlocking)
                    {
                        var isPermanent = settings.AutoPermanentBlacklistEnabled;
                        var duration = settings.IpBlockDurationMinutes > 0 ? settings.IpBlockDurationMinutes : 30;
                        await _audit.BlockIpAsync(ip, reason: $"Error spike: {count}/min at {path}", durationMinutes: duration, isPermanent: isPermanent);

                        // If permanent, log a dedicated event
                        if (isPermanent)
                        {
                            await _audit.LogSecurityEventAsync(
                                eventType: "IpBlacklistedPermanent",
                                severity: "Critical",
                                ipAddress: ip,
                                details: $"Auto-permanent blacklist due to error spike. Count={count}/min, Path={path}");
                        }
                    }
                }
            }
        }
    }
}
