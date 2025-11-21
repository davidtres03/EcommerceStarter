using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Models;
using EcommerceStarter.Models.Mobile;
using EcommerceStarter.Services;
using EcommerceStarter.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    /// <summary>
    /// Mobile API Controller
    /// Provides RESTful endpoints for Android/iOS mobile applications
    /// All endpoints require authorization via JWT bearer token
    /// </summary>
    [ApiController]
    [Route("api/mobile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MobileApiController : ControllerBase
    {
        private readonly ILogger<MobileApiController> _logger;

        public MobileApiController(ILogger<MobileApiController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard metrics for mobile display
        /// Returns: pending orders, revenue, website status, traffic data, etc.
        /// Cache-Control: max-age=300 (5 minutes)
        /// </summary>
        [HttpGet("dashboard")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<MobileDashboardDto>> GetDashboard()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Mobile dashboard requested by user: {UserId}", userId);

                // TODO: Implement dashboard service integration
                // This should fetch:
                // - Pending orders count
                // - Today's revenue
                // - Website status (online/offline)
                // - Traffic data (24-hour history)
                // - Inventory alert count
                // - Support queue length

                var response = new MobileDashboardDto
                {
                    Timestamp = DateTime.UtcNow,
                    Metrics = new DashboardMetricsDto
                    {
                        PendingOrders = 0,
                        TodayRevenue = 0,
                        TodayOrders = 0,
                        WebsiteStatus = "online",
                        LastStatusCheck = DateTime.UtcNow,
                        Traffic = new TrafficDataDto { Last24Hours = new(), Peak = 0, Average = 0 },
                        InventoryAlerts = 0,
                        SupportQueueLength = 0,
                        Uptime = 99.9m,
                        LastBackupTime = DateTime.UtcNow.AddHours(-6)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching mobile dashboard");
                return StatusCode(500, new { error = "Failed to fetch dashboard" });
            }
        }

        /// <summary>
        /// Get list of recent orders (paginated)
        /// Query params: limit (default 20), offset (default 0)
        /// </summary>
        [HttpGet("orders")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<MobileOrderListDto>> GetOrders(
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (limit > 100) limit = 100; // Max limit safety
                if (offset < 0) offset = 0;

                _logger.LogInformation("Mobile orders requested: limit={Limit}, offset={Offset}, user={UserId}",
                    limit, offset, userId);

                // TODO: Implement order service integration
                // This should fetch recent orders with pagination

                var response = new MobileOrderListDto
                {
                    Orders = new(),
                    TotalCount = 0,
                    HasMore = false
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching mobile orders");
                return StatusCode(500, new { error = "Failed to fetch orders" });
            }
        }

        /// <summary>
        /// Get full order details
        /// </summary>
        [HttpGet("orders/{orderId}")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<MobileOrderDetailDto>> GetOrderDetail(string orderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(orderId, out var orderGuid))
                {
                    return BadRequest(new { error = "Invalid order ID" });
                }

                _logger.LogInformation("Mobile order detail requested: orderId={OrderId}, user={UserId}", orderId, userId);

                // TODO: Implement order detail fetching

                return NotFound(new { error = "Order not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order detail: {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to fetch order" });
            }
        }

        /// <summary>
        /// Update order status (mark as shipped, cancelled, etc.)
        /// </summary>
        [HttpPut("orders/{orderId}/status")]
        public async Task<ActionResult> UpdateOrderStatus(
            string orderId,
            [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(orderId, out var orderGuid))
                {
                    return BadRequest(new { error = "Invalid order ID" });
                }

                _logger.LogInformation("Mobile order status update: orderId={OrderId}, newStatus={Status}, user={UserId}",
                    orderId, request.Status, userId);

                // TODO: Implement order status update
                // This should update order status and optionally set tracking number

                return Ok(new { message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to update order" });
            }
        }

        /// <summary>
        /// Process chat message with AI
        /// Optionally include dashboard context for smarter responses
        /// </summary>
        [HttpPost("ai/chat")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MobileChatResponseDto>> ProcessChatMessage(
            [FromBody] MobileChatRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                _logger.LogInformation("Mobile chat message: user={UserId}, messageLength={Length}",
                    userId, request.Message.Length);

                // TODO: Implement AI chat processing
                // This should:
                // - Get dashboard context if requested
                // - Process message through Claude/Ollama
                // - Store in database for history
                // - Return response with token count

                var response = new MobileChatResponseDto
                {
                    ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                    Response = "AI integration pending",
                    TokensUsed = 0,
                    Model = "claude-3-sonnet",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { error = "Failed to process message" });
            }
        }

        /// <summary>
        /// Process voice input (transcribe and respond with AI)
        /// Input: Audio file (.wav, max 30 seconds)
        /// Output: Transcription + AI response + audio response (optional)
        /// </summary>
        [HttpPost("ai/process-voice")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MobileVoiceResponseDto>> ProcessVoiceInput(
            [FromForm] IFormFile audio,
            [FromForm] bool includeAudio = false,
            [FromForm] bool includeContext = false)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (audio == null || audio.Length == 0)
                {
                    return BadRequest(new { error = "Audio file is required" });
                }

                if (audio.Length > 30 * 1024 * 1024) // 30 MB max
                {
                    return BadRequest(new { error = "Audio file too large (max 30 MB)" });
                }

                _logger.LogInformation("Mobile voice input: user={UserId}, audioSize={Size}",
                    userId, audio.Length);

                // TODO: Implement voice processing
                // This should:
                // - Transcribe audio using Google Speech-to-Text
                // - Process transcription as chat message
                // - Optionally generate audio response

                var result = new MobileVoiceResponseDto
                {
                    Transcription = "Voice processing pending",
                    Confidence = 0,
                    AiResponse = "Voice feature not yet implemented",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing voice input");
                return StatusCode(500, new { error = "Failed to process voice" });
            }
        }

        /// <summary>
        /// Get chat history (paginated)
        /// </summary>
        [HttpGet("ai/chat-history")]
        [Authorize(Roles = "Admin")]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<MobileChatHistoryDto>> GetChatHistory(
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (limit > 100) limit = 100;
                if (offset < 0) offset = 0;

                // TODO: Implement chat history retrieval from database

                var response = new MobileChatHistoryDto
                {
                    Messages = new(),
                    TotalCount = 0,
                    HasMore = false
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chat history");
                return StatusCode(500, new { error = "Failed to fetch chat history" });
            }
        }

        /// <summary>
        /// Register push notification token for device
        /// Called on app startup to enable push notifications
        /// </summary>
        [HttpPost("push-tokens")]
        public async Task<ActionResult> RegisterPushToken(
            [FromBody] PushTokenRegistrationRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("Push token registered: user={UserId}, deviceId={DeviceId}, os={OS}",
                    userId, request.Token, request.DeviceId, request.OS);

                // TODO: Store push token in database
                // This enables backend to send push notifications to this device

                return Ok(new { message = "Push token registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering push token");
                return StatusCode(500, new { error = "Failed to register push token" });
            }
        }

        /// <summary>
        /// Get user profile info (for settings screen)
        /// </summary>
        [HttpGet("user/profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // TODO: Fetch user profile from database

                var profile = new UserProfileDto
                {
                    Id = userId ?? "",
                    Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                    DisplayName = User.Identity?.Name ?? "User",
                    Role = "Owner",
                    CreatedAt = DateTime.UtcNow
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile");
                return StatusCode(500, new { error = "Failed to fetch profile" });
            }
        }

        /// <summary>
        /// Check app version for updates
        /// </summary>
        [HttpGet("app/version-check")]
        [AllowAnonymous]
        public async Task<ActionResult<VersionCheckResponseDto>> CheckAppVersion(
            [FromQuery] string currentVersion)
        {
            try
            {
                // TODO: Fetch latest app version from database
                // Compare with current version

                var response = new VersionCheckResponseDto
                {
                    UpdateAvailable = false,
                    LatestVersion = currentVersion,
                    ReleaseNotes = "No updates available",
                    Message = "You are up to date"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking app version");
                return StatusCode(500, new { error = "Failed to check version" });
            }
        }
    }
}
