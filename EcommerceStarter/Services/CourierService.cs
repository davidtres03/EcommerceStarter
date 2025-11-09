using System.Text.RegularExpressions;
using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for detecting courier/carrier from tracking number format
    /// </summary>
    public interface ICourierService
    {
        /// <summary>
        /// Detect courier from tracking number format
        /// Returns Courier.Unknown if cannot be determined
        /// </summary>
        Courier DetectCourierFromTrackingNumber(string trackingNumber);

        /// <summary>
        /// Get all available couriers
        /// </summary>
        IEnumerable<Courier> GetAvailableCouriers();

        /// <summary>
        /// Check if tracking number looks valid for a specific courier
        /// </summary>
        bool IsValidForCourier(string trackingNumber, Courier courier);
    }

    /// <summary>
    /// Implementation of courier detection service
    /// Detects courier based on tracking number format patterns
    /// </summary>
    public class CourierService : ICourierService
    {
        /// <summary>
        /// Detect courier from tracking number format
        /// 
        /// Patterns:
        /// - USPS: 20-22 digits, sometimes with format like 9400xxxxxxxxxxxxxxxx
        /// - UPS: Starts with 1Z followed by 16 alphanumeric characters (1Z + 16 chars = 18 total)
        /// - FedEx: 12-14 digits or 34-digit format
        /// </summary>
        public Courier DetectCourierFromTrackingNumber(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return Courier.Unknown;
            }

            trackingNumber = trackingNumber.Trim().ToUpperInvariant();

            // UPS: Starts with 1Z, followed by 16 alphanumeric characters
            // Total: 1Z + 16 = 18 characters
            if (IsValidForCourier(trackingNumber, Courier.UPS))
            {
                return Courier.UPS;
            }

            // USPS: 20-22 digit format, often starts with 9400 or 9200
            if (IsValidForCourier(trackingNumber, Courier.USPS))
            {
                return Courier.USPS;
            }

            // FedEx: 12-14 digit format or 34 digit format
            if (IsValidForCourier(trackingNumber, Courier.FedEx))
            {
                return Courier.FedEx;
            }

            // Could not determine
            return Courier.Unknown;
        }

        /// <summary>
        /// Get all available couriers for dropdown
        /// </summary>
        public IEnumerable<Courier> GetAvailableCouriers()
        {
            return new[]
            {
                Courier.UPS,
                Courier.USPS,
                Courier.FedEx
            };
        }

        /// <summary>
        /// Check if tracking number matches the pattern for a specific courier
        /// </summary>
        public bool IsValidForCourier(string trackingNumber, Courier courier)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return false;
            }

            trackingNumber = trackingNumber.Trim().ToUpperInvariant();

            return courier switch
            {
                Courier.UPS => IsValidUPSFormat(trackingNumber),
                Courier.USPS => IsValidUSPSFormat(trackingNumber),
                Courier.FedEx => IsValidFedExFormat(trackingNumber),
                _ => false
            };
        }

        /// <summary>
        /// Check if tracking number matches UPS format
        /// UPS format: 1Z followed by 16 alphanumeric characters
        /// Example: 1Z999AA10123456784
        /// </summary>
        private bool IsValidUPSFormat(string trackingNumber)
        {
            // UPS format: 1Z + 16 alphanumeric = 18 characters total
            // Also accepts longer variants
            if (!trackingNumber.StartsWith("1Z"))
            {
                return false;
            }

            // Must be at least 18 characters (1Z + 16 alphanumeric)
            // But can be longer for international shipments
            if (trackingNumber.Length < 18)
            {
                return false;
            }

            // Check if the rest are alphanumeric
            string rest = trackingNumber.Substring(2);
            return Regex.IsMatch(rest, @"^[A-Z0-9]+$");
        }

        /// <summary>
        /// Check if tracking number matches USPS format
        /// USPS formats:
        /// - 20-22 digit format (Intelligent Mail Barcode)
        /// - Often starts with 9400 or 9200
        /// - Can be 20-22 digits
        /// Examples: 9400111899223456789012, 9200123456789012345678
        /// </summary>
        private bool IsValidUSPSFormat(string trackingNumber)
        {
            // USPS is typically 20-22 digits
            if (trackingNumber.Length < 20 || trackingNumber.Length > 22)
            {
                return false;
            }

            // Must be all digits
            if (!Regex.IsMatch(trackingNumber, @"^\d+$"))
            {
                return false;
            }

            // Usually starts with 9 (Intelligent Mail Barcode)
            if (!trackingNumber.StartsWith("9"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if tracking number matches FedEx format
        /// FedEx formats:
        /// - 12-14 digit format (standard)
        /// - 34 digit format (international)
        /// Examples: 794698743434, 0015956178, 123456789012345678901234567890123456
        /// </summary>
        private bool IsValidFedExFormat(string trackingNumber)
        {
            // FedEx is typically 12-14 digits or 34 digits
            if (trackingNumber.Length >= 12 && trackingNumber.Length <= 14)
            {
                // Standard FedEx format - all digits
                return Regex.IsMatch(trackingNumber, @"^\d+$");
            }

            if (trackingNumber.Length == 34)
            {
                // International format - can be alphanumeric
                return Regex.IsMatch(trackingNumber, @"^[A-Z0-9]+$");
            }

            return false;
        }
    }
}
