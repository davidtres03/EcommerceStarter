using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public int ErrorStatusCode { get; set; }
        public string ErrorTitle { get; set; } = "Error";
        public string ErrorMessage { get; set; } = "An error occurred while processing your request.";

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            ErrorStatusCode = statusCode ?? HttpContext.Response.StatusCode;

            // Set custom messages based on status code
            switch (ErrorStatusCode)
            {
                case 400:
                    ErrorTitle = "400 - Bad Request";
                    ErrorMessage = "The request could not be understood by the server.";
                    break;
                case 401:
                    ErrorTitle = "401 - Unauthorized";
                    ErrorMessage = "You need to be logged in to access this page.";
                    break;
                case 403:
                    ErrorTitle = "403 - Forbidden";
                    ErrorMessage = "You don't have permission to access this resource.";
                    break;
                case 404:
                    ErrorTitle = "404 - Page Not Found";
                    ErrorMessage = "The page you're looking for doesn't exist.";
                    break;
                case 500:
                    ErrorTitle = "500 - Server Error";
                    ErrorMessage = "Something went wrong on our end. We're working to fix it.";
                    break;
                case 503:
                    ErrorTitle = "503 - Service Unavailable";
                    ErrorMessage = "The service is temporarily unavailable. Please try again later.";
                    break;
                default:
                    ErrorTitle = $"{ErrorStatusCode} - Error";
                    ErrorMessage = "An unexpected error occurred.";
                    break;
            }
        }
    }
}
