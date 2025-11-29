using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceStarter.Models;
using EcommerceStarter.Models.Tracking;
using EcommerceStarter.Services;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceStarter.Services.Tracking
{
    /// <summary>
    /// USPS Developer API tracking provider (OAuth 2.0)
    /// Fetches real-time tracking data from modern USPS API
    /// </summary>
    public class UspsTrackingProvider : ICarrierTrackingProvider
    {
        private readonly IApiConfigurationService _apiConfigService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UspsTrackingProvider> _logger;
        private readonly IMemoryCache _cache;
        
        // Cache for USPS configuration
        private DateTime _configLoadedAt = DateTime.MinValue;
        private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromMinutes(5);
        private ApiConfiguration? _cachedUspsConfig;
        private Dictionary<string, string?>? _cachedDecryptedValues;
        private string? _cachedMetadataJson;

        // USPS API endpoints - CORRECTED TO USE v3 (GET-based)
        private const string USPS_PRODUCTION_AUTH_URL = "https://apis.usps.com/oauth2/v3/token";
        private const string USPS_PRODUCTION_TRACKING_URL = "https://apis.usps.com/tracking/v3/tracking";
        
        private const string USPS_SANDBOX_AUTH_URL = "https://apis-tem.usps.com/oauth2/v3/token";
        private const string USPS_SANDBOX_TRACKING_URL = "https://apis-tem.usps.com/tracking/v3/tracking";

        // Cache key for OAuth token
        private const string OAUTH_TOKEN_CACHE_KEY = "usps_oauth_token";

        public UspsTrackingProvider(
            IApiConfigurationService apiConfigService,
            IHttpClientFactory httpClientFactory,
            ILogger<UspsTrackingProvider> logger,
            IMemoryCache cache)
        {
            _apiConfigService = apiConfigService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cache = cache;
        }

        private async Task EnsureConfigurationLoadedAsync()
        {
            if (DateTime.UtcNow - _configLoadedAt < ConfigCacheDuration && _cachedUspsConfig != null)
            {
                return; // Cache is still valid
            }

            // Load USPS configuration from database
            var uspsConfigs = await _apiConfigService.GetConfigurationsByTypeAsync("USPS", activeOnly: true);
            _cachedUspsConfig = uspsConfigs.OrderBy(c => c.Id).FirstOrDefault();

            if (_cachedUspsConfig != null)
            {
                // Load decrypted values
                _cachedDecryptedValues = await _apiConfigService.GetDecryptedValuesAsync(_cachedUspsConfig.Id);
                _cachedMetadataJson = _cachedUspsConfig.MetadataJson;
            }

            _configLoadedAt = DateTime.UtcNow;
        }

        private async Task<bool> GetUspsEnabledAsync()
        {
            await EnsureConfigurationLoadedAsync();
            
            if (_cachedUspsConfig == null) return false;
            
            // Parse metadata for enabled flag
            if (!string.IsNullOrEmpty(_cachedMetadataJson))
            {
                try
                {
                    var metadata = JsonDocument.Parse(_cachedMetadataJson);
                    if (metadata.RootElement.TryGetProperty("enabled", out var enabledProp))
                    {
                        return enabledProp.GetBoolean();
                    }
                }
                catch { /* Ignore JSON parse errors */ }
            }
            
            return _cachedUspsConfig.IsActive;
        }

        private async Task<bool> GetUspsUseSandboxAsync()
        {
            await EnsureConfigurationLoadedAsync();
            
            if (_cachedUspsConfig == null) return false;
            
            // Parse metadata for sandbox flag
            if (!string.IsNullOrEmpty(_cachedMetadataJson))
            {
                try
                {
                    var metadata = JsonDocument.Parse(_cachedMetadataJson);
                    if (metadata.RootElement.TryGetProperty("useSandbox", out var sandboxProp))
                    {
                        return sandboxProp.GetBoolean();
                    }
                }
                catch { /* Ignore JSON parse errors */ }
            }
            
            return _cachedUspsConfig.IsTestMode;
        }

        private async Task<string?> GetUspsUserIdAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return _cachedDecryptedValues?.GetValueOrDefault("Value1");
        }

        private async Task<string?> GetUspsPasswordAsync()
        {
            await EnsureConfigurationLoadedAsync();
            return _cachedDecryptedValues?.GetValueOrDefault("Value2");
        }

        public Courier SupportedCourier => Courier.USPS;

        public async Task<bool> IsEnabledAsync()
        {
            var enabled = await GetUspsEnabledAsync();
            var userId = await GetUspsUserIdAsync();
            var password = await GetUspsPasswordAsync();
            
            return enabled && 
                   !string.IsNullOrEmpty(userId) && // Consumer Key
                   !string.IsNullOrEmpty(password); // Consumer Secret
        }

        public async Task<TrackingStatus?> GetStatusAsync(string trackingNumber)
        {
            try
            {
                // Check if enabled
                if (!await IsEnabledAsync())
                {
                    _logger.LogWarning("USPS tracking is not enabled or configured");
                    return null;
                }

                // Get OAuth token
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Failed to obtain USPS OAuth access token");
                    return null;
                }

                // Call USPS tracking API
                var trackingData = await CallUspsTrackingApiAsync(trackingNumber, accessToken);
                if (trackingData == null || !trackingData.HasValue)
                {
                    return null;
                }

                // Parse response
                var trackingStatus = ParseUspsResponse(trackingData.Value, trackingNumber);

                return trackingStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching USPS tracking status for {TrackingNumber}", trackingNumber);
                return null;
            }
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                // Check cache first
                if (_cache.TryGetValue<string>(OAUTH_TOKEN_CACHE_KEY, out var cachedToken))
                {
                    _logger.LogInformation("Using cached USPS OAuth token");
                    return cachedToken;
                }

                // Get credentials and settings
                var useSandbox = await GetUspsUseSandboxAsync();
                var consumerKey = await GetUspsUserIdAsync(); // Consumer Key
                var consumerSecret = await GetUspsPasswordAsync(); // Consumer Secret

                if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret))
                {
                    _logger.LogError("USPS Consumer Key or Secret not configured");
                    return null;
                }

                // Trim whitespace from credentials (common copy/paste issue)
                consumerKey = consumerKey.Trim();
                consumerSecret = consumerSecret.Trim();

                // Determine which endpoint to use
                var authUrl = useSandbox ? USPS_SANDBOX_AUTH_URL : USPS_PRODUCTION_AUTH_URL;
                var environment = useSandbox ? "SANDBOX" : "PRODUCTION";

                // LOG FULL CREDENTIALS FOR DEBUGGING (REMOVE IN PRODUCTION!)
                _logger.LogWarning("=== USPS CREDENTIALS DEBUG (REMOVE THIS IN PRODUCTION!) ===");
                _logger.LogWarning("USPS Environment: {Environment}", environment);
                _logger.LogWarning("USPS Auth URL: {AuthUrl}", authUrl);
                _logger.LogWarning("USPS Consumer Key (FULL): {ConsumerKey}", consumerKey);
                _logger.LogWarning("USPS Consumer Key Length: {KeyLength}", consumerKey.Length);
                _logger.LogWarning("USPS Consumer Secret (FULL): {ConsumerSecret}", consumerSecret);
                _logger.LogWarning("USPS Consumer Secret Length: {SecretLength}", consumerSecret.Length);
                _logger.LogWarning("=== END CREDENTIALS DEBUG ===");

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // Build OAuth request with JSON body (not form-encoded)
                var oauthBody = new
                {
                    client_id = consumerKey,
                    client_secret = consumerSecret,
                    grant_type = "client_credentials"
                };

                var jsonBody = JsonSerializer.Serialize(oauthBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, authUrl)
                {
                    Content = content
                };

                // Add required headers
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Requesting USPS OAuth token from {Environment} at {Url}", environment, authUrl);

                var response = await client.SendAsync(request);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("USPS OAuth response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("USPS OAuth failed with status {StatusCode}: {ResponseBody}", 
                        response.StatusCode, responseContent);
                    
                    // Parse error for specific messages
                    try
                    {
                        var errorDoc = JsonDocument.Parse(responseContent);
                        if (errorDoc.RootElement.TryGetProperty("error_description", out var errorDesc))
                        {
                            var description = errorDesc.GetString();
                            if (description?.Contains("not approved") == true || description?.Contains("inactive") == true)
                            {
                                _logger.LogError("USPS account may not be approved yet. Check your USPS Developer Portal status.");
                            }
                            else if (description?.Contains("invalid") == true)
                            {
                                _logger.LogError("Credentials may be for wrong environment. Current: {Environment}. Try toggling Sandbox setting.", environment);
                            }
                        }
                    }
                    catch { /* Ignore JSON parse errors */ }
                    
                    return null;
                }

                _logger.LogInformation("USPS OAuth response body: {ResponseBody}", responseContent);

                var tokenData = JsonSerializer.Deserialize<UspsOAuthResponse>(responseContent);

                if (tokenData?.AccessToken == null)
                {
                    _logger.LogError("USPS OAuth response missing access token. Response: {Response}", responseContent);
                    return null;
                }

                // Cache token (expires in ~1 hour, cache for 50 minutes to be safe)
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(50)
                };

                _cache.Set(OAUTH_TOKEN_CACHE_KEY, tokenData.AccessToken, cacheOptions);

                _logger.LogInformation("USPS OAuth token obtained and cached from {Environment} (expires in {Expires} seconds)", environment, tokenData.ExpiresIn);

                return tokenData.AccessToken;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error obtaining USPS OAuth token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining USPS OAuth token");
                return null;
            }
        }

        private async Task<JsonElement?> CallUspsTrackingApiAsync(string trackingNumber, string accessToken)
        {
            try
            {
                // Get settings to determine environment
                var useSandbox = await GetUspsUseSandboxAsync();
                var trackingUrl = useSandbox ? USPS_SANDBOX_TRACKING_URL : USPS_PRODUCTION_TRACKING_URL;
                var environment = useSandbox ? "SANDBOX" : "PRODUCTION";

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // USPS v3 API: GET request with tracking number in URL and query parameters
                // Official format: https://apis.usps.com/tracking/v3/tracking/{TrackingNumber}?expand=DETAIL
                var trackingUri = $"{trackingUrl}/{trackingNumber}?expand=DETAIL";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, trackingUri);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                httpRequest.Headers.Accept.Clear();
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Calling USPS Tracking API ({Environment}) v3 GET for {TrackingNumber}: {Url}", 
                    environment, trackingNumber, trackingUri);

                var trackingResponse = await client.SendAsync(httpRequest);

                var responseJson = await trackingResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("USPS Tracking API response status: {StatusCode}", trackingResponse.StatusCode);

                if (!trackingResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("USPS Tracking API failed with status {StatusCode}: {ResponseBody}", 
                        trackingResponse.StatusCode, responseJson);
                    return null;
                }

                _logger.LogInformation("USPS Tracking API response body: {ResponseBody}", responseJson);

                var jsonDoc = JsonDocument.Parse(responseJson);

                _logger.LogInformation("USPS Tracking API response received successfully");

                return jsonDoc.RootElement;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling USPS Tracking API");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "USPS Tracking API request timeout");
                return null;
            }
        }

        private TrackingStatus? ParseUspsResponse(JsonElement response, string trackingNumber)
        {
            try
            {
                var trackingStatus = new TrackingStatus
                {
                    TrackingNumber = trackingNumber,
                    Courier = Courier.USPS,
                    Events = new List<TrackingEvent>()
                };

                // Parse tracking summary
                if (response.TryGetProperty("trackingSummary", out var summary))
                {
                    var status = summary.GetProperty("status").GetString() ?? "";
                    var statusCategory = summary.GetProperty("statusCategory").GetString() ?? "";
                    
                    trackingStatus.CurrentStatus = status;
                    trackingStatus.StatusType = MapUspsStatusToStandardStatus(statusCategory, status);

                    if (summary.TryGetProperty("eventTimestamp", out var timestamp))
                    {
                        trackingStatus.LastUpdate = DateTime.Parse(timestamp.GetString() ?? "");
                    }
                }

                // Parse tracking events
                if (response.TryGetProperty("trackingEvents", out var events))
                {
                    foreach (var evt in events.EnumerateArray())
                    {
                        var eventTime = evt.TryGetProperty("eventTimestamp", out var ts) 
                            ? DateTime.Parse(ts.GetString() ?? "") 
                            : DateTime.Now;
                        
                        var eventStatus = evt.TryGetProperty("eventType", out var et) 
                            ? et.GetString() ?? "" 
                            : "";
                        
                        var eventCity = evt.TryGetProperty("eventCity", out var ec) 
                            ? ec.GetString() 
                            : null;
                        
                        var eventState = evt.TryGetProperty("eventState", out var es) 
                            ? es.GetString() 
                            : null;

                        trackingStatus.Events.Add(new TrackingEvent
                        {
                            Timestamp = eventTime,
                            Status = eventStatus,
                            City = eventCity,
                            State = eventState,
                            Location = BuildLocation(eventCity, eventState),
                            Description = eventStatus
                        });
                    }
                }

                // Set location from most recent event
                if (trackingStatus.Events.Any())
                {
                    var latest = trackingStatus.Events.OrderByDescending(e => e.Timestamp).First();
                    trackingStatus.City = latest.City;
                    trackingStatus.State = latest.State;
                    trackingStatus.LastLocation = latest.Location;
                }

                // Check if delivered
                if (trackingStatus.StatusType == TrackingStatusType.Delivered)
                {
                    trackingStatus.DeliveredAt = trackingStatus.LastUpdate;
                }

                _logger.LogInformation(
                    "Parsed USPS tracking for {TrackingNumber}: {Status} ({EventCount} events)",
                    trackingNumber,
                    trackingStatus.CurrentStatus,
                    trackingStatus.Events.Count);

                return trackingStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing USPS response JSON");
                return null;
            }
        }

        private TrackingStatusType MapUspsStatusToStandardStatus(string statusCategory, string status)
        {
            var category = statusCategory.ToLower();
            var statusText = status.ToLower();

            // Map based on status category
            if (category.Contains("delivered"))
                return TrackingStatusType.Delivered;

            if (category.Contains("out for delivery"))
                return TrackingStatusType.OutForDelivery;

            if (category.Contains("in transit") || category.Contains("arrived") || category.Contains("departed"))
                return TrackingStatusType.InTransit;

            if (category.Contains("accepted") || category.Contains("picked up"))
                return TrackingStatusType.PickedUp;

            if (category.Contains("exception") || category.Contains("alert"))
                return TrackingStatusType.Exception;

            if (category.Contains("return"))
                return TrackingStatusType.Returned;

            // Fallback to status text
            if (statusText.Contains("delivered"))
                return TrackingStatusType.Delivered;

            if (statusText.Contains("out for delivery"))
                return TrackingStatusType.OutForDelivery;

            return TrackingStatusType.InTransit;
        }

        private string? BuildLocation(string? city, string? state)
        {
            if (string.IsNullOrEmpty(city) && string.IsNullOrEmpty(state))
                return null;

            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state))
                return $"{city}, {state}";

            return city ?? state;
        }

        // OAuth response model
        private class UspsOAuthResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
