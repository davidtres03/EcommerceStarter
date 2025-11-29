using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Policy = "AdminOnly")]
    public class SecurityModel : PageModel
    {
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly EcommerceStarter.Data.ApplicationDbContext _dbContext;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ILogger<SecurityModel> _logger;
        private readonly EcommerceStarter.Services.Analytics.IAnalyticsExclusionMetricsService? _exclusionMetrics;

        public SecurityModel(
            ISecuritySettingsService securitySettingsService, 
            ILogger<SecurityModel> logger,
            EcommerceStarter.Data.ApplicationDbContext dbContext,
            ISecurityAuditService securityAuditService,
            EcommerceStarter.Services.Analytics.IAnalyticsExclusionMetricsService? exclusionMetrics = null)
        {
            _securitySettingsService = securitySettingsService;
            _logger = logger;
            _dbContext = dbContext;
            _securityAuditService = securityAuditService;
            _exclusionMetrics = exclusionMetrics;
        }

        [BindProperty]
        public SecuritySettings Settings { get; set; } = new();

        public int WhitelistExclusionsLast24h { get; set; }
        public int PrivateExclusionsLast24h { get; set; }
        public int AdminPageExclusionsLast24h { get; set; }

        public async Task OnGetAsync()
        {
            Settings = await _securitySettingsService.GetSettingsAsync();
            if (_exclusionMetrics != null)
            {
                var (wl, pr, admin) = _exclusionMetrics.GetCountsSince(TimeSpan.FromHours(24));
                WhitelistExclusionsLast24h = wl;
                PrivateExclusionsLast24h = pr;
                AdminPageExclusionsLast24h = admin;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userEmail = User.Identity?.Name ?? "Unknown Admin";
                await _securitySettingsService.UpdateSettingsAsync(Settings, userEmail);

                TempData["SuccessMessage"] = "Security settings updated successfully!";
                _logger.LogInformation("Security settings updated by {User}", userEmail);
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating security settings");
                TempData["ErrorMessage"] = "Failed to update security settings. Please try again.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResetToDefaultsAsync()
        {
            try
            {
                var defaultSettings = new SecuritySettings();
                var userEmail = User.Identity?.Name ?? "Unknown Admin";
                await _securitySettingsService.UpdateSettingsAsync(defaultSettings, userEmail);

                TempData["SuccessMessage"] = "Security settings reset to defaults successfully!";
                _logger.LogInformation("Security settings reset to defaults by {User}", userEmail);
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting security settings");
                TempData["ErrorMessage"] = "Failed to reset security settings. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostPurgeAnalyticsAsync()
        {
            try
            {
                // Count before delete
                var sessions = _dbContext.VisitorSessions.Count();
                var pageViews = _dbContext.PageViews.Count();
                var eventsCount = _dbContext.VisitorEvents.Count();

                // Delete children first, then sessions
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM [PageViews]");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM [VisitorEvents]");
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM [VisitorSessions]");

                // Audit log
                var adminEmail = User.Identity?.Name ?? "Unknown Admin";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var details = $"Purged analytics by {adminEmail}. Sessions={sessions}, PageViews={pageViews}, Events={eventsCount}";
                await _securityAuditService.LogSecurityEventAsync(
                    eventType: "AnalyticsPurged",
                    severity: "Info",
                    ipAddress: ip,
                    userEmail: adminEmail,
                    details: details,
                    endpoint: "/Admin/Settings/Security#PurgeAnalytics");

                TempData["SuccessMessage"] = "Analytics data purged successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purging analytics data");
                TempData["ErrorMessage"] = "Failed to purge analytics. Please check logs.";
                return RedirectToPage();
            }
        }
    }
}
