using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Admin.Security
{
    [Authorize(Policy = "AdminOnly")]
    public class AuditLogModel : PageModel
    {
        private readonly ISecurityAuditService _securityAuditService;

        public AuditLogModel(ISecurityAuditService securityAuditService)
        {
            _securityAuditService = securityAuditService;
        }

        public List<SecurityAuditLog> RecentEvents { get; set; } = new();
        public List<BlockedIp> BlockedIps { get; set; } = new();

        [BindProperty]
        public string? IpToUnblock { get; set; }

        public async Task OnGetAsync()
        {
            RecentEvents = await _securityAuditService.GetRecentSecurityEventsAsync(200);
            BlockedIps = await _securityAuditService.GetBlockedIpsAsync();
        }

        public async Task<IActionResult> OnPostUnblockIpAsync()
        {
            if (!string.IsNullOrEmpty(IpToUnblock))
            {
                await _securityAuditService.UnblockIpAsync(IpToUnblock);
                TempData["SuccessMessage"] = $"IP {IpToUnblock} has been unblocked.";
            }

            return RedirectToPage();
        }
    }
}
