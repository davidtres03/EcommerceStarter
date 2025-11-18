using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminUsersModel> _logger;

        public AdminUsersModel(UserManager<ApplicationUser> userManager, ILogger<AdminUsersModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public List<ApplicationUser> AdminUsers { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAdminUsers();
        }

        public async Task<IActionResult> OnPostRemoveAdminAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToPage();
            }

            // Prevent removing yourself
            if (user.Id == _userManager.GetUserId(User))
            {
                SuccessMessage = "You cannot remove your own admin privileges.";
                return RedirectToPage();
            }

            // Check if this is the last admin
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count <= 1)
            {
                SuccessMessage = "Cannot remove the last admin user. There must always be at least one admin.";
                return RedirectToPage();
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                // Optionally add them to Customer role
                await _userManager.AddToRoleAsync(user, "Customer");
                
                _logger.LogInformation($"Admin role removed from {user.Email} by {User.Identity?.Name}");
                SuccessMessage = $"Admin privileges removed from {user.Email}. User has been converted to Customer role.";
            }
            else
            {
                SuccessMessage = "Failed to remove admin privileges.";
            }

            return RedirectToPage();
        }

        private async Task LoadAdminUsers()
        {
            var allUsers = await _userManager.Users.ToListAsync();
            AdminUsers = new List<ApplicationUser>();

            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    AdminUsers.Add(user);
                }
            }

            AdminUsers = AdminUsers.OrderBy(u => u.CreatedAt).ToList();
        }
    }
}
