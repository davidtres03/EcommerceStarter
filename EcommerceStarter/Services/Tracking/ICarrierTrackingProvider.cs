using EcommerceStarter.Models;
using EcommerceStarter.Models.Tracking;

namespace EcommerceStarter.Services.Tracking
{
    /// <summary>
    /// Interface for carrier-specific tracking providers
    /// Each carrier (USPS, UPS, FedEx) implements this
    /// </summary>
    public interface ICarrierTrackingProvider
    {
        /// <summary>
        /// Which courier this provider supports
        /// </summary>
        Courier SupportedCourier { get; }

        /// <summary>
        /// Fetch tracking status from carrier API
        /// </summary>
        Task<TrackingStatus?> GetStatusAsync(string trackingNumber);

        /// <summary>
        /// Check if this provider is configured and enabled
        /// </summary>
        Task<bool> IsEnabledAsync();
    }
}
