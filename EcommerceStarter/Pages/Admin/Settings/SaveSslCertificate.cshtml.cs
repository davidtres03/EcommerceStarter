using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class SaveSslCertificateModel : PageModel
    {
        private readonly ISslConfigService _sslConfigService;
        private readonly ILogger<SaveSslCertificateModel> _logger;

        public SaveSslCertificateModel(
            ISslConfigService sslConfigService,
            ILogger<SaveSslCertificateModel> logger)
        {
            _sslConfigService = sslConfigService;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Domain name is required")]
        public string DomainName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Certificate is required")]
        [MinLength(100, ErrorMessage = "Certificate appears to be invalid (too short)")]
        public string Certificate { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Private key is required")]
        [MinLength(100, ErrorMessage = "Private key appears to be invalid (too short)")]
        public string PrivateKey { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Issuer is required")]
        public string Issuer { get; set; } = "Cloudflare";

        [BindProperty]
        public DateTime? ExpirationDate { get; set; }

        public SslConfiguration? ExistingCertificate { get; set; }

        public async Task OnGetAsync()
        {
            ExistingCertificate = await _sslConfigService.GetActiveCertificateAsync();
            
            // Pre-populate expiration date if it's a new Cloudflare certificate (15 years)
            if (ExistingCertificate == null)
            {
                ExpirationDate = DateTime.UtcNow.AddYears(15);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ExistingCertificate = await _sslConfigService.GetActiveCertificateAsync();
                return Page();
            }

            try
            {
                // Validate certificate format
                if (!Certificate.TrimStart().StartsWith("-----BEGIN CERTIFICATE-----"))
                {
                    ModelState.AddModelError("Certificate", "Certificate must be in PEM format (should start with -----BEGIN CERTIFICATE-----)");
                    ExistingCertificate = await _sslConfigService.GetActiveCertificateAsync();
                    return Page();
                }

                // Validate private key format
                if (!PrivateKey.TrimStart().StartsWith("-----BEGIN") || 
                    (!PrivateKey.Contains("PRIVATE KEY") && !PrivateKey.Contains("RSA PRIVATE KEY")))
                {
                    ModelState.AddModelError("PrivateKey", "Private key must be in PEM format (should start with -----BEGIN PRIVATE KEY----- or -----BEGIN RSA PRIVATE KEY-----)");
                    ExistingCertificate = await _sslConfigService.GetActiveCertificateAsync();
                    return Page();
                }

                // If no expiration date provided and it's Cloudflare, default to 15 years
                if (!ExpirationDate.HasValue && Issuer.Equals("Cloudflare", StringComparison.OrdinalIgnoreCase))
                {
                    ExpirationDate = DateTime.UtcNow.AddYears(15);
                }

                var userEmail = User.Identity?.Name ?? "Unknown";

                var success = await _sslConfigService.SaveCertificateAsync(
                    Certificate.Trim(),
                    PrivateKey.Trim(),
                    DomainName.Trim(),
                    Issuer.Trim(),
                    ExpirationDate,
                    userEmail
                );

                if (success)
                {
                    _logger.LogInformation($"SSL certificate saved successfully for domain: {DomainName} by {userEmail}");
                    TempData["SuccessMessage"] = $"SSL certificate saved successfully for {DomainName}! Certificate and private key are encrypted in the database.";
                    return RedirectToPage();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to save certificate. Please check the logs.");
                    ExistingCertificate = await _sslConfigService.GetActiveCertificateAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving SSL certificate");
                ModelState.AddModelError(string.Empty, $"An error occurred while saving the certificate: {ex.Message}");
                ExistingCertificate = await _sslConfigService.GetActiveCertificateAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetDownloadCertificateAsync()
        {
            try
            {
                var certificate = await _sslConfigService.GetCertificateAsync();
                
                if (string.IsNullOrEmpty(certificate))
                {
                    TempData["ErrorMessage"] = "No certificate found to download.";
                    return RedirectToPage();
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(certificate);
                var fileName = $"certificate-{DateTime.UtcNow:yyyyMMdd}.pem";
                
                _logger.LogInformation($"Certificate downloaded by {User.Identity?.Name}");
                
                return File(bytes, "application/x-pem-file", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate");
                TempData["ErrorMessage"] = "Failed to download certificate.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetDownloadPrivateKeyAsync()
        {
            try
            {
                var privateKey = await _sslConfigService.GetPrivateKeyAsync();
                
                if (string.IsNullOrEmpty(privateKey))
                {
                    TempData["ErrorMessage"] = "No private key found to download.";
                    return RedirectToPage();
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(privateKey);
                var fileName = $"private-key-{DateTime.UtcNow:yyyyMMdd}.pem";
                
                _logger.LogWarning($"?? SECURITY ALERT: Private key downloaded by {User.Identity?.Name}");
                
                return File(bytes, "application/x-pem-file", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading private key");
                TempData["ErrorMessage"] = "Failed to download private key.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetDownloadBothAsync()
        {
            try
            {
                var certificate = await _sslConfigService.GetCertificateAsync();
                var privateKey = await _sslConfigService.GetPrivateKeyAsync();
                
                if (string.IsNullOrEmpty(certificate) || string.IsNullOrEmpty(privateKey))
                {
                    TempData["ErrorMessage"] = "Certificate or private key not found.";
                    return RedirectToPage();
                }

                // Create a combined PEM file
                var combined = $"{certificate}\n{privateKey}";
                var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
                var fileName = $"ssl-bundle-{DateTime.UtcNow:yyyyMMdd}.pem";
                
                _logger.LogWarning($"?? SECURITY ALERT: SSL bundle (cert + key) downloaded by {User.Identity?.Name}");
                
                return File(bytes, "application/x-pem-file", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading SSL bundle");
                TempData["ErrorMessage"] = "Failed to download SSL bundle.";
                return RedirectToPage();
            }
        }
    }
}
