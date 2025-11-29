using EcommerceStarter.Models;
using EcommerceStarter.Models.Tracking;

namespace EcommerceStarter.Services.Tracking
{
    /// <summary>
    /// Service for fetching real-time tracking status from carriers
    /// Includes caching to prevent API abuse
    /// </summary>
    public interface ITrackingStatusService
    {
        /// <summary>
        /// Get tracking status for a specific tracking number and courier
        /// Results are cached for 10 minutes
        /// </summary>
        Task<TrackingStatus?> GetTrackingStatusAsync(Courier courier, string trackingNumber);

        /// <summary>
        /// Check if package has been delivered
        /// </summary>
        Task<bool> IsDeliveredAsync(Courier courier, string trackingNumber);

        /// <summary>
        /// Force refresh tracking status (bypasses cache)
        /// </summary>
        Task<TrackingStatus?> RefreshTrackingStatusAsync(Courier courier, string trackingNumber);

        /// <summary>
        /// Check if a specific courier is enabled and configured
        /// </summary>
        Task<bool> IsEnabledAsync(Courier courier);
    }
}
