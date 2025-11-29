using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    public interface ISslConfigService
    {
        Task<string> GetCertificateAsync();
        Task<string> GetPrivateKeyAsync();
        Task<bool> SaveCertificateAsync(string certificate, string privateKey, string domainName, string issuer, DateTime? expirationDate, string updatedBy);
        Task<SslConfiguration?> GetActiveCertificateAsync();
        Task<bool> IsConfiguredAsync();
    }

    public class SslConfigService : ISslConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<SslConfigService> _logger;

        public SslConfigService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            ILogger<SslConfigService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<string> GetCertificateAsync()
        {
            try
            {
                var config = await _context.SslConfigurations
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.LastUpdated)
                    .FirstOrDefaultAsync();

                if (config == null || string.IsNullOrEmpty(config.EncryptedCertificate))
                {
                    _logger.LogWarning("No SSL certificate found in database");
                    return string.Empty;
                }

                return _encryptionService.Decrypt(config.EncryptedCertificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL certificate from database");
                return string.Empty;
            }
        }

        public async Task<string> GetPrivateKeyAsync()
        {
            try
            {
                var config = await _context.SslConfigurations
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.LastUpdated)
                    .FirstOrDefaultAsync();

                if (config == null || string.IsNullOrEmpty(config.EncryptedPrivateKey))
                {
                    _logger.LogWarning("No SSL private key found in database");
                    return string.Empty;
                }

                return _encryptionService.Decrypt(config.EncryptedPrivateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL private key from database");
                return string.Empty;
            }
        }

        public async Task<bool> SaveCertificateAsync(
            string certificate,
            string privateKey,
            string domainName,
            string issuer,
            DateTime? expirationDate,
            string updatedBy)
        {
            try
            {
                // Deactivate all existing certificates for this domain
                var existingCerts = await _context.SslConfigurations
                    .Where(s => s.DomainName == domainName && s.IsActive)
                    .ToListAsync();

                foreach (var cert in existingCerts)
                {
                    cert.IsActive = false;
                    cert.LastUpdated = DateTime.UtcNow;
                }

                // Create new configuration
                var newConfig = new SslConfiguration
                {
                    EncryptedCertificate = _encryptionService.Encrypt(certificate),
                    EncryptedPrivateKey = _encryptionService.Encrypt(privateKey),
                    DomainName = domainName,
                    Issuer = issuer,
                    ExpirationDate = expirationDate,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    UpdatedBy = updatedBy,
                    IsActive = true
                };

                _context.SslConfigurations.Add(newConfig);

                // Create audit log
                var auditLog = new SslConfigurationAuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = existingCerts.Any() ? "Updated" : "Created",
                    DomainName = domainName,
                    UserEmail = updatedBy,
                    Changes = $"Certificate updated for {domainName}, expires: {expirationDate?.ToShortDateString() ?? "Unknown"}"
                };

                _context.SslConfigurationAuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"SSL certificate saved successfully for domain: {domainName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving SSL certificate to database");
                return false;
            }
        }

        public async Task<SslConfiguration?> GetActiveCertificateAsync()
        {
            try
            {
                return await _context.SslConfigurations
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.LastUpdated)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active SSL certificate");
                return null;
            }
        }

        public async Task<bool> IsConfiguredAsync()
        {
            try
            {
                var config = await _context.SslConfigurations
                    .Where(s => s.IsActive)
                    .AnyAsync();

                return config;
            }
            catch
            {
                return false;
            }
        }
    }
}
