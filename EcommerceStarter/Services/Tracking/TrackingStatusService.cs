using EcommerceStarter.Models;
using EcommerceStarter.Models.Tracking;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceStarter.Services.Tracking
{
    /// <summary>
    /// Main tracking status service with caching
    /// Coordinates multiple carrier providers
    /// </summary>
    public class TrackingStatusService : ITrackingStatusService
    {
        private readonly IMemoryCache _cache;
        private readonly IEnumerable<ICarrierTrackingProvider> _providers;
        private readonly ILogger<TrackingStatusService> _logger;

        // Cache settings
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        public TrackingStatusService(
            IMemoryCache cache,
            IEnumerable<ICarrierTrackingProvider> providers,
            ILogger<TrackingStatusService> logger)
        {
            _cache = cache;
            _providers = providers;
            _logger = logger;
        }

        public async Task<TrackingStatus?> GetTrackingStatusAsync(Courier courier, string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
            {
                _logger.LogWarning("Tracking number is empty");
                return null;
            }

            // Check cache first
            var cacheKey = GetCacheKey(courier, trackingNumber);
            
            if (_cache.TryGetValue<TrackingStatus>(cacheKey, out var cachedStatus))
            {
                _logger.LogInformation(
                    "Returning cached tracking status for {Courier} {TrackingNumber}",
                    courier,
                    trackingNumber);
                return cachedStatus;
            }

            // Cache miss - fetch from provider
            var status = await FetchFromProviderAsync(courier, trackingNumber);

            if (status != null)
            {
                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration,
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };

                _cache.Set(cacheKey, status, cacheOptions);

                _logger.LogInformation(
                    "Cached tracking status for {Courier} {TrackingNumber} for {Duration} minutes",
                    courier,
                    trackingNumber,
                    _cacheDuration.TotalMinutes);
            }

            return status;
        }

        public async Task<bool> IsDeliveredAsync(Courier courier, string trackingNumber)
        {
            var status = await GetTrackingStatusAsync(courier, trackingNumber);
            return status?.IsDelivered ?? false;
        }

        public async Task<TrackingStatus?> RefreshTrackingStatusAsync(Courier courier, string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
            {
                return null;
            }

            // Remove from cache to force refresh
            var cacheKey = GetCacheKey(courier, trackingNumber);
            _cache.Remove(cacheKey);

            _logger.LogInformation(
                "Forcing refresh for {Courier} {TrackingNumber}",
                courier,
                trackingNumber);

            // Fetch fresh data
            return await GetTrackingStatusAsync(courier, trackingNumber);
        }

        public async Task<bool> IsEnabledAsync(Courier courier)
        {
            try
            {
                // Find the provider for this courier
                var provider = _providers.FirstOrDefault(p => p.SupportedCourier == courier);

                if (provider == null)
                {
                    _logger.LogWarning("No provider found for courier {Courier}", courier);
                    return false;
                }

                // Check if provider is enabled
                return await provider.IsEnabledAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if {Courier} is enabled", courier);
                return false;
            }
        }

        private async Task<TrackingStatus?> FetchFromProviderAsync(Courier courier, string trackingNumber)
        {
            try
            {
                // Find the provider for this courier
                var provider = _providers.FirstOrDefault(p => p.SupportedCourier == courier);

                if (provider == null)
                {
                    _logger.LogWarning("No provider found for courier {Courier}", courier);
                    return null;
                }

                // Check if provider is enabled
                if (!await provider.IsEnabledAsync())
                {
                    _logger.LogWarning("Provider for {Courier} is not enabled", courier);
                    return null;
                }

                _logger.LogInformation(
                    "Fetching tracking status from {Courier} for {TrackingNumber}",
                    courier,
                    trackingNumber);

                var status = await provider.GetStatusAsync(trackingNumber);

                if (status == null)
                {
                    _logger.LogWarning(
                        "Provider for {Courier} returned null status for {TrackingNumber}",
                        courier,
                        trackingNumber);
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching tracking status for {Courier} {TrackingNumber}",
                    courier,
                    trackingNumber);
                return null;
            }
        }

        private string GetCacheKey(Courier courier, string trackingNumber)
        {
            return $"tracking_{courier}_{trackingNumber}";
        }
    }
}
