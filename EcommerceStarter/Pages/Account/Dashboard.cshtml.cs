using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect to Orders page as the primary dashboard view
            return RedirectToPage("/Orders/Index");
        }
    }
}
