using EcommerceStarter.Models;

namespace EcommerceStarter.Services.Mobile
{
    /// <summary>
    /// Service for handling mobile app data requests
    /// Provides aggregated dashboard data, orders, and business logic
    /// </summary>
    public interface IMobileDashboardService
    {
        Task<DashboardData> GetDashboardAsync(string userId);
        Task<List<int>> GetTrafficDataAsync(int hoursBack);
        Task<int> GetSupportQueueLengthAsync();
        Task<int> GetInventoryAlertCountAsync();
    }

    /// <summary>
    /// Service for managing orders (mobile endpoints)
    /// Handles order retrieval, status updates, and fulfillment
    /// </summary>
    public interface IMobileOrderService
    {
        Task<List<Order>> GetRecentOrdersAsync(int limit, int offset);
        Task<int> GetTotalOrderCountAsync();
        Task<Order?> GetOrderByIdAsync(Guid orderId);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, string status, string? trackingNumber);
    }

    /// <summary>
    /// Service for AI operations on mobile
    /// Handles chat messages, transcription, and AI integration
    /// </summary>
    public interface IMobileAIService
    {
        Task<ChatResponse> ProcessChatMessageAsync(string message, string userId, string? conversationId, string? context = null);
        Task<TranscriptionResult> TranscribeAudioAsync(Stream audioStream);
        Task<byte[]> GenerateSpeechAsync(string text);
        Task<List<ChatHistory>> GetChatHistoryAsync(string userId, int limit, int offset);
        Task<int> GetChatHistoryCountAsync(string userId);
    }

    /// <summary>
    /// Service for checking website status and health
    /// </summary>
    public interface IMobileWebsiteStatusService
    {
        Task<StatusCheckResult> CheckStatusAsync();
        Task<List<StatusCheckResult>> GetStatusHistoryAsync(int hoursBack);
    }

    // Data Models
    public class DashboardData
    {
        public int PendingOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayOrdersCount { get; set; }
        public int InventoryAlertCount { get; set; }
        public decimal Uptime { get; set; }
        public DateTime? LastBackupTime { get; set; }
    }

    public class StatusCheckResult
    {
        public bool IsOnline { get; set; }
        public DateTime CheckedAt { get; set; }
        public int ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ChatResponse
    {
        public string ConversationId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public string Model { get; set; } = string.Empty;
    }

    public class TranscriptionResult
    {
        public string TranscribedText { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public int DurationMs { get; set; }
    }

    public class ChatHistory
    {
        public string Id { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
