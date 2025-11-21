using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/settings")]
    public class SettingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsApiController> _logger;

        public SettingsApiController(ApplicationDbContext context, ILogger<SettingsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/settings/branding
        [HttpGet("branding")]
        public async Task<IActionResult> GetBrandingSettings()
        {
            try
            {
                var settings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Branding settings not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        // Site Identity
                        siteName = settings.SiteName,
                        siteTagline = settings.SiteTagline,
                        siteIcon = settings.SiteIcon,
                        logoUrl = settings.LogoUrl,
                        faviconUrl = settings.FaviconUrl,
                        heroImageUrl = settings.HeroImageUrl,

                        // Colors & Fonts
                        primaryColor = settings.PrimaryColor,
                        primaryDark = settings.PrimaryDark,
                        primaryLight = settings.PrimaryLight,
                        secondaryColor = settings.SecondaryColor,
                        accentColor = settings.AccentColor,
                        primaryFont = settings.PrimaryFont,
                        headingFont = settings.HeadingFont,

                        // Business Info
                        companyName = settings.CompanyName,
                        contactEmail = settings.ContactEmail,
                        supportEmail = settings.SupportEmail,
                        address = settings.Address,
                        city = settings.City,
                        state = settings.State,
                        country = settings.Country,

                        // Social Media
                        facebookUrl = settings.FacebookUrl,
                        twitterUrl = settings.TwitterUrl,
                        instagramUrl = settings.InstagramUrl,
                        linkedInUrl = settings.LinkedInUrl,

                        // SEO
                        metaDescription = settings.MetaDescription,
                        metaKeywords = settings.MetaKeywords
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branding settings");
                return StatusCode(500, new { success = false, message = "Error retrieving branding settings" });
            }
        }

        // PUT: api/settings/branding
        [HttpPut("branding")]
        public async Task<IActionResult> UpdateBrandingSettings([FromBody] UpdateBrandingRequest request)
        {
            try
            {
                var settings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Branding settings not found" });
                }

                // Update Site Identity
                if (!string.IsNullOrWhiteSpace(request.SiteName))
                    settings.SiteName = request.SiteName;

                if (request.SiteTagline != null)
                    settings.SiteTagline = request.SiteTagline;

                if (request.SiteIcon != null)
                    settings.SiteIcon = request.SiteIcon;

                if (request.LogoUrl != null)
                    settings.LogoUrl = request.LogoUrl;

                if (request.FaviconUrl != null)
                    settings.FaviconUrl = request.FaviconUrl;

                if (request.HeroImageUrl != null)
                    settings.HeroImageUrl = request.HeroImageUrl;

                // Update Colors & Fonts
                if (!string.IsNullOrWhiteSpace(request.PrimaryColor))
                    settings.PrimaryColor = request.PrimaryColor;

                if (!string.IsNullOrWhiteSpace(request.PrimaryDark))
                    settings.PrimaryDark = request.PrimaryDark;

                if (!string.IsNullOrWhiteSpace(request.PrimaryLight))
                    settings.PrimaryLight = request.PrimaryLight;

                if (!string.IsNullOrWhiteSpace(request.SecondaryColor))
                    settings.SecondaryColor = request.SecondaryColor;

                if (!string.IsNullOrWhiteSpace(request.AccentColor))
                    settings.AccentColor = request.AccentColor;

                if (!string.IsNullOrWhiteSpace(request.PrimaryFont))
                    settings.PrimaryFont = request.PrimaryFont;

                if (!string.IsNullOrWhiteSpace(request.HeadingFont))
                    settings.HeadingFont = request.HeadingFont;

                // Update Business Info
                if (!string.IsNullOrWhiteSpace(request.CompanyName))
                    settings.CompanyName = request.CompanyName;

                if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                    settings.ContactEmail = request.ContactEmail;

                if (request.SupportEmail != null)
                    settings.SupportEmail = request.SupportEmail;

                if (request.Address != null)
                    settings.Address = request.Address;

                if (request.City != null)
                    settings.City = request.City;

                if (request.State != null)
                    settings.State = request.State;

                if (request.Country != null)
                    settings.Country = request.Country;

                // Update Social Media
                if (request.FacebookUrl != null)
                    settings.FacebookUrl = request.FacebookUrl;

                if (request.TwitterUrl != null)
                    settings.TwitterUrl = request.TwitterUrl;

                if (request.InstagramUrl != null)
                    settings.InstagramUrl = request.InstagramUrl;

                if (request.LinkedInUrl != null)
                    settings.LinkedInUrl = request.LinkedInUrl;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Branding settings updated");

                return Ok(new
                {
                    success = true,
                    message = "Branding settings updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branding settings");
                return StatusCode(500, new { success = false, message = "Error updating branding settings" });
            }
        }

        // GET: api/settings/security
        [HttpGet("security")]
        public async Task<IActionResult> GetSecuritySettings()
        {
            try
            {
                var settings = await _context.SecuritySettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Security settings not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        // Rate Limiting
                        rateLimiting = new
                        {
                            enabled = settings.EnableRateLimiting,
                            maxRequestsPerMinute = settings.MaxRequestsPerMinute,
                            maxRequestsPerSecond = settings.MaxRequestsPerSecond,
                            maxRequestsPerMinuteAuth = settings.MaxRequestsPerMinuteAuth,
                            maxRequestsPerSecondAuth = settings.MaxRequestsPerSecondAuth,
                            exemptAdmins = settings.ExemptAdminsFromRateLimiting
                        },

                        // IP Blocking
                        ipBlocking = new
                        {
                            enabled = settings.EnableIpBlocking,
                            maxFailedLoginAttempts = settings.MaxFailedLoginAttempts,
                            failedLoginWindowMinutes = settings.FailedLoginWindowMinutes,
                            blockDurationMinutes = settings.IpBlockDurationMinutes
                        },

                        // Account Lockout
                        accountLockout = new
                        {
                            enabled = settings.EnableAccountLockout,
                            maxAttempts = settings.AccountLockoutMaxAttempts,
                            durationMinutes = settings.AccountLockoutDurationMinutes
                        },

                        // Audit Logging
                        auditLogging = new
                        {
                            enabled = settings.EnableSecurityAuditLogging,
                            retentionDays = settings.AuditLogRetentionDays
                        },

                        // Notifications
                        notifications = new
                        {
                            notifyOnCriticalEvents = settings.NotifyOnCriticalEvents,
                            notifyOnIpBlocking = settings.NotifyOnIpBlocking,
                            notificationEmail = settings.NotificationEmail
                        },

                        // Advanced
                        advanced = new
                        {
                            enableGeoIpBlocking = settings.EnableGeoIpBlocking,
                            blockedCountries = settings.BlockedCountries,
                            whitelistedIps = settings.WhitelistedIps,
                            blacklistedIps = settings.BlacklistedIps
                        },

                        lastModified = settings.LastModified,
                        lastModifiedBy = settings.LastModifiedBy
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security settings");
                return StatusCode(500, new { success = false, message = "Error retrieving security settings" });
            }
        }

        // PUT: api/settings/security
        [HttpPut("security")]
        public async Task<IActionResult> UpdateSecuritySettings([FromBody] UpdateSecurityRequest request)
        {
            try
            {
                var settings = await _context.SecuritySettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Security settings not found" });
                }

                // Update Rate Limiting
                if (request.EnableRateLimiting.HasValue)
                    settings.EnableRateLimiting = request.EnableRateLimiting.Value;

                if (request.MaxRequestsPerMinute.HasValue)
                    settings.MaxRequestsPerMinute = request.MaxRequestsPerMinute.Value;

                if (request.MaxRequestsPerSecond.HasValue)
                    settings.MaxRequestsPerSecond = request.MaxRequestsPerSecond.Value;

                if (request.MaxRequestsPerMinuteAuth.HasValue)
                    settings.MaxRequestsPerMinuteAuth = request.MaxRequestsPerMinuteAuth.Value;

                if (request.MaxRequestsPerSecondAuth.HasValue)
                    settings.MaxRequestsPerSecondAuth = request.MaxRequestsPerSecondAuth.Value;

                if (request.ExemptAdminsFromRateLimiting.HasValue)
                    settings.ExemptAdminsFromRateLimiting = request.ExemptAdminsFromRateLimiting.Value;

                // Update IP Blocking
                if (request.EnableIpBlocking.HasValue)
                    settings.EnableIpBlocking = request.EnableIpBlocking.Value;

                if (request.MaxFailedLoginAttempts.HasValue)
                    settings.MaxFailedLoginAttempts = request.MaxFailedLoginAttempts.Value;

                if (request.FailedLoginWindowMinutes.HasValue)
                    settings.FailedLoginWindowMinutes = request.FailedLoginWindowMinutes.Value;

                if (request.IpBlockDurationMinutes.HasValue)
                    settings.IpBlockDurationMinutes = request.IpBlockDurationMinutes.Value;

                // Update Account Lockout
                if (request.EnableAccountLockout.HasValue)
                    settings.EnableAccountLockout = request.EnableAccountLockout.Value;

                if (request.AccountLockoutMaxAttempts.HasValue)
                    settings.AccountLockoutMaxAttempts = request.AccountLockoutMaxAttempts.Value;

                if (request.AccountLockoutDurationMinutes.HasValue)
                    settings.AccountLockoutDurationMinutes = request.AccountLockoutDurationMinutes.Value;

                // Update Audit Logging
                if (request.EnableSecurityAuditLogging.HasValue)
                    settings.EnableSecurityAuditLogging = request.EnableSecurityAuditLogging.Value;

                if (request.AuditLogRetentionDays.HasValue)
                    settings.AuditLogRetentionDays = request.AuditLogRetentionDays.Value;

                // Update Notifications
                if (request.NotifyOnCriticalEvents.HasValue)
                    settings.NotifyOnCriticalEvents = request.NotifyOnCriticalEvents.Value;

                if (request.NotifyOnIpBlocking.HasValue)
                    settings.NotifyOnIpBlocking = request.NotifyOnIpBlocking.Value;

                if (request.NotificationEmail != null)
                    settings.NotificationEmail = request.NotificationEmail;

                // Update Advanced Settings
                if (request.EnableGeoIpBlocking.HasValue)
                    settings.EnableGeoIpBlocking = request.EnableGeoIpBlocking.Value;

                if (request.BlockedCountries != null)
                    settings.BlockedCountries = request.BlockedCountries;

                if (request.WhitelistedIps != null)
                    settings.WhitelistedIps = request.WhitelistedIps;

                if (request.BlacklistedIps != null)
                    settings.BlacklistedIps = request.BlacklistedIps;

                settings.LastModified = DateTime.UtcNow;
                settings.LastModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Security settings updated");

                return Ok(new
                {
                    success = true,
                    message = "Security settings updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating security settings");
                return StatusCode(500, new { success = false, message = "Error updating security settings" });
            }
        }

        // GET: api/settings/api-configurations
        [HttpGet("api-configurations")]
        public async Task<IActionResult> GetApiConfigurations()
        {
            try
            {
                var configurations = await _context.ApiConfigurations
                    .OrderBy(c => c.ApiType)
                    .ThenBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.ApiType,
                        c.Name,
                        c.IsActive,
                        c.IsTestMode,
                        c.Description,
                        HasCredentials = !string.IsNullOrEmpty(c.EncryptedValue1) || !string.IsNullOrEmpty(c.EncryptedValue2),
                        c.LastValidated,
                        c.CreatedAt,
                        c.LastUpdated,
                        c.CreatedBy,
                        c.UpdatedBy
                    })
                    .ToListAsync();

                // Group by API type for easier display
                var groupedConfigs = configurations
                    .GroupBy(c => c.ApiType)
                    .Select(g => new
                    {
                        apiType = g.Key,
                        configurations = g.ToList(),
                        count = g.Count()
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = groupedConfigs,
                    totalCount = configurations.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API configurations");
                return StatusCode(500, new { success = false, message = "Error retrieving API configurations" });
            }
        }

        // GET: api/settings/api-configurations/{id}
        [HttpGet("api-configurations/{id}")]
        public async Task<IActionResult> GetApiConfiguration(int id)
        {
            try
            {
                var config = await _context.ApiConfigurations
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.ApiType,
                        c.Name,
                        c.IsActive,
                        c.IsTestMode,
                        c.Description,
                        c.MetadataJson,
                        HasValue1 = !string.IsNullOrEmpty(c.EncryptedValue1),
                        HasValue2 = !string.IsNullOrEmpty(c.EncryptedValue2),
                        HasValue3 = !string.IsNullOrEmpty(c.EncryptedValue3),
                        HasValue4 = !string.IsNullOrEmpty(c.EncryptedValue4),
                        HasValue5 = !string.IsNullOrEmpty(c.EncryptedValue5),
                        c.LastValidated,
                        c.CreatedAt,
                        c.LastUpdated,
                        c.CreatedBy,
                        c.UpdatedBy
                    })
                    .FirstOrDefaultAsync();

                if (config == null)
                {
                    return NotFound(new { success = false, message = "API configuration not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API configuration {ConfigId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving API configuration" });
            }
        }

        // PUT: api/settings/api-configurations/{id}/toggle
        [HttpPut("api-configurations/{id}/toggle")]
        public async Task<IActionResult> ToggleApiConfiguration(int id)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(id);

                if (config == null)
                {
                    return NotFound(new { success = false, message = "API configuration not found" });
                }

                config.IsActive = !config.IsActive;
                config.LastUpdated = DateTime.UtcNow;
                config.UpdatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("API configuration {ConfigId} ({ApiType}/{Name}) toggled to {Status}",
                    id, config.ApiType, config.Name, config.IsActive ? "Active" : "Inactive");

                return Ok(new
                {
                    success = true,
                    message = $"API configuration {(config.IsActive ? "activated" : "deactivated")} successfully",
                    data = new
                    {
                        config.Id,
                        config.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling API configuration {ConfigId}", id);
                return StatusCode(500, new { success = false, message = "Error toggling API configuration" });
            }
        }

        // GET: api/settings/email
        [HttpGet("email")]
        public async Task<IActionResult> GetEmailSettings()
        {
            try
            {
                var settings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Email settings not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        fromName = settings.CompanyName,
                        fromEmail = settings.ContactEmail,
                        replyToEmail = settings.SupportEmail,
                        footerText = settings.SiteTagline
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email settings");
                return StatusCode(500, new { success = false, message = "Error retrieving email settings" });
            }
        }

        // GET: api/settings/timezones/available
        [HttpGet("timezones/available")]
        public IActionResult GetAvailableTimezones()
        {
            try
            {
                var timezones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new
                    {
                        id = tz.Id,
                        displayName = tz.DisplayName,
                        standardName = tz.StandardName,
                        daylightName = tz.DaylightName,
                        baseUtcOffset = tz.BaseUtcOffset.ToString(@"hh\:mm"),
                        supportsDaylightSavingTime = tz.SupportsDaylightSavingTime
                    })
                    .OrderBy(tz => tz.baseUtcOffset)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = timezones,
                    count = timezones.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available timezones");
                return StatusCode(500, new { success = false, message = "Error retrieving timezones" });
            }
        }

        // GET: api/settings/timezone
        [HttpGet("timezone")]
        public async Task<IActionResult> GetTimezoneSettings()
        {
            try
            {
                var settings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Settings not found" });
                }

                TimeZoneInfo? currentTimeZone = null;
                try
                {
                    currentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
                }
                catch
                {
                    // If configured timezone is invalid, fall back to system local
                    currentTimeZone = TimeZoneInfo.Local;
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        timeZoneId = settings.TimeZoneId,
                        displayName = currentTimeZone.DisplayName,
                        standardName = currentTimeZone.StandardName,
                        baseUtcOffset = currentTimeZone.BaseUtcOffset.ToString(@"hh\:mm"),
                        supportsDaylightSavingTime = currentTimeZone.SupportsDaylightSavingTime,
                        currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, currentTimeZone).ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timezone settings");
                return StatusCode(500, new { success = false, message = "Error retrieving timezone settings" });
            }
        }

        // PUT: api/settings/timezone
        [HttpPut("timezone")]
        public async Task<IActionResult> UpdateTimezone([FromBody] UpdateTimezoneRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.TimeZoneId))
                {
                    return BadRequest(new { success = false, message = "TimeZoneId is required" });
                }

                // Validate timezone ID
                TimeZoneInfo? timeZone;
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return BadRequest(new { success = false, message = $"Invalid timezone ID: {request.TimeZoneId}" });
                }

                var settings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "Settings not found" });
                }

                settings.TimeZoneId = request.TimeZoneId;
                settings.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Timezone updated to {TimeZoneId}", request.TimeZoneId);

                return Ok(new
                {
                    success = true,
                    message = "Timezone updated successfully",
                    data = new
                    {
                        timeZoneId = settings.TimeZoneId,
                        displayName = timeZone.DisplayName,
                        currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timezone");
                return StatusCode(500, new { success = false, message = "Error updating timezone" });
            }
        }
    }

    // Request DTOs
    public class UpdateTimezoneRequest
    {
        public string TimeZoneId { get; set; } = string.Empty;
    }

    public class UpdateBrandingRequest
    {
        public string? SiteName { get; set; }
        public string? SiteTagline { get; set; }
        public string? SiteIcon { get; set; }
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? PrimaryDark { get; set; }
        public string? PrimaryLight { get; set; }
        public string? SecondaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? PrimaryFont { get; set; }
        public string? HeadingFont { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? SupportEmail { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? LinkedInUrl { get; set; }
    }

    public class UpdateSecurityRequest
    {
        // Rate Limiting
        public bool? EnableRateLimiting { get; set; }
        public int? MaxRequestsPerMinute { get; set; }
        public int? MaxRequestsPerSecond { get; set; }
        public int? MaxRequestsPerMinuteAuth { get; set; }
        public int? MaxRequestsPerSecondAuth { get; set; }
        public bool? ExemptAdminsFromRateLimiting { get; set; }

        // IP Blocking
        public bool? EnableIpBlocking { get; set; }
        public int? MaxFailedLoginAttempts { get; set; }
        public int? FailedLoginWindowMinutes { get; set; }
        public int? IpBlockDurationMinutes { get; set; }

        // Account Lockout
        public bool? EnableAccountLockout { get; set; }
        public int? AccountLockoutMaxAttempts { get; set; }
        public int? AccountLockoutDurationMinutes { get; set; }

        // Audit Logging
        public bool? EnableSecurityAuditLogging { get; set; }
        public int? AuditLogRetentionDays { get; set; }

        // Notifications
        public bool? NotifyOnCriticalEvents { get; set; }
        public bool? NotifyOnIpBlocking { get; set; }
        public string? NotificationEmail { get; set; }

        // Advanced
        public bool? EnableGeoIpBlocking { get; set; }
        public string? BlockedCountries { get; set; }
        public string? WhitelistedIps { get; set; }
        public string? BlacklistedIps { get; set; }
    }
}
