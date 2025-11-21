using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(ISiteSettingsService siteSettingsService, ILogger<ContactModel> logger)
        {
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        [BindProperty]
        [Required]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string Subject { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string Message { get; set; } = string.Empty;

        public bool MessageSent { get; set; }
        public string SupportEmail { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            SupportEmail = settings.SupportEmail;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                SupportEmail = settings.SupportEmail;
                return Page();
            }

            // Log the contact form submission
            _logger.LogInformation("Contact form submitted: {Name} <{Email}> - {Subject}", Name, Email, Subject);

            // In a production environment, you would:
            // 1. Send an email to support
            // 2. Store the message in a database
            // 3. Create a support ticket
            
            MessageSent = true;
            return Page();
        }
    }
}
