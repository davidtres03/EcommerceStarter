namespace EcommerceStarter.Models
{
    /// <summary>
    /// Supported shipping carriers/couriers
    /// </summary>
    public enum Courier
    {
        /// <summary>
        /// Unknown or not yet determined
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// United States Postal Service
        /// </summary>
        USPS = 1,
        
        /// <summary>
        /// United Parcel Service
        /// </summary>
        UPS = 2,
        
        /// <summary>
        /// Federal Express
        /// </summary>
        FedEx = 3
    }

    /// <summary>
    /// Helper methods for Courier enum
    /// </summary>
    public static class CourierExtensions
    {
        /// <summary>
        /// Get display name for courier
        /// </summary>
        public static string GetDisplayName(this Courier courier) => courier switch
        {
            Courier.USPS => "USPS (United States Postal Service)",
            Courier.UPS => "UPS (United Parcel Service)",
            Courier.FedEx => "FedEx (Federal Express)",
            _ => "Unknown"
        };

        /// <summary>
        /// Get short name for courier
        /// </summary>
        public static string GetShortName(this Courier courier) => courier switch
        {
            Courier.USPS => "USPS",
            Courier.UPS => "UPS",
            Courier.FedEx => "FedEx",
            _ => "Unknown"
        };

        /// <summary>
        /// Generate tracking URL for the given courier and tracking number
        /// </summary>
        public static string GenerateTrackingUrl(this Courier courier, string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return string.Empty;
            }

            return courier switch
            {
                Courier.USPS => $"https://tools.usps.com/go/TrackConfirmAction?tLabels={Uri.EscapeDataString(trackingNumber)}",
                Courier.UPS => $"https://www.ups.com/track?tracknum={Uri.EscapeDataString(trackingNumber)}",
                Courier.FedEx => $"https://tracking.fedex.com/en/tracking/{Uri.EscapeDataString(trackingNumber)}",
                _ => string.Empty
            };
        }
    }
}
