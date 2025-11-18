namespace EcommerceStarter.Models.Mobile
{
    /// <summary>
    /// Mobile Dashboard Response DTO
    /// </summary>
    public class MobileDashboardDto
    {
        public DateTime Timestamp { get; set; }
        public DashboardMetricsDto Metrics { get; set; }
    }

    public class DashboardMetricsDto
    {
        public int PendingOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public string WebsiteStatus { get; set; } // "online" or "offline"
        public DateTime LastStatusCheck { get; set; }
        public TrafficDataDto Traffic { get; set; }
        public int InventoryAlerts { get; set; }
        public int SupportQueueLength { get; set; }
        public decimal Uptime { get; set; }
        public DateTime? LastBackupTime { get; set; }
    }

    public class TrafficDataDto
    {
        public List<int> Last24Hours { get; set; } = new();
        public int Peak { get; set; }
        public int Average { get; set; }
    }

    /// <summary>
    /// Mobile Orders List Response DTO
    /// </summary>
    public class MobileOrderListDto
    {
        public List<MobileOrderSummaryDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class MobileOrderSummaryDto
    {
        public string Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Mobile Order Detail Response DTO
    /// </summary>
    public class MobileOrderDetailDto
    {
        public string Id { get; set; }
        public string OrderNumber { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public CustomerInfoDto Customer { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public ShippingInfoDto Shipping { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public string Notes { get; set; }
    }

    public class CustomerInfoDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }

    public class ShippingInfoDto
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
    }

    /// <summary>
    /// Update Order Status Request DTO
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; }
        public string TrackingNumber { get; set; }
    }

    /// <summary>
    /// Mobile Chat Request DTO
    /// </summary>
    public class MobileChatRequestDto
    {
        public string Message { get; set; }
        public string ConversationId { get; set; }
        public bool IncludeContext { get; set; } = false;
    }

    /// <summary>
    /// Mobile Chat Response DTO
    /// </summary>
    public class MobileChatResponseDto
    {
        public string ConversationId { get; set; }
        public string Response { get; set; }
        public int TokensUsed { get; set; }
        public string Model { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Mobile Voice Input Processing Request
    /// </summary>
    public class MobileVoiceRequestDto
    {
        public IFormFile Audio { get; set; }
        public bool IncludeAudio { get; set; } = false;
        public bool IncludeContext { get; set; } = false;
    }

    /// <summary>
    /// Mobile Voice Response DTO
    /// </summary>
    public class MobileVoiceResponseDto
    {
        public string Transcription { get; set; }
        public decimal Confidence { get; set; }
        public string AiResponse { get; set; }
        public string AudioResponse { get; set; } // Base64 encoded MP3
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Chat History Response DTO
    /// </summary>
    public class MobileChatHistoryDto
    {
        public List<ChatMessageDto> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class ChatMessageDto
    {
        public string Id { get; set; }
        public string Role { get; set; } // "user" or "assistant"
        public string Content { get; set; }
        public int TokensUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Push Token Registration Request DTO
    /// </summary>
    public class PushTokenRegistrationRequest
    {
        public string Token { get; set; }
        public string DeviceId { get; set; }
        public string OS { get; set; } // "android" or "ios"
        public string OSVersion { get; set; } // e.g., "14.0"
        public string AppVersion { get; set; } // e.g., "1.0.0"
    }

    /// <summary>
    /// Push Token Entity for database storage
    /// </summary>
    public class PushToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string Token { get; set; }
        public string DeviceId { get; set; }
        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string AppVersion { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastUsedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// User Profile DTO (for settings screen)
    /// </summary>
    public class UserProfileDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// App Version Check Response DTO
    /// </summary>
    public class VersionCheckResponseDto
    {
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// App Version Entity for database storage
    /// </summary>
    public class AppVersion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Version { get; set; } // e.g., "1.0.0"
        public string Platform { get; set; } // "android", "ios", "web"
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public bool IsRequired { get; set; } = false;
        public int? MinimumOSVersion { get; set; }
    }
}
