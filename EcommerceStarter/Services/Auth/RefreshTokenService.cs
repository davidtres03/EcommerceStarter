using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services.Auth
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> CreateRefreshTokenAsync(string userId, string token, string ipAddress, string userAgent);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task<bool> ValidateRefreshTokenAsync(string token, string userId);
        Task RevokeRefreshTokenAsync(string token);
        Task RevokeAllUserTokensAsync(string userId);
        Task CleanupExpiredTokensAsync();
    }

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            ApplicationDbContext context, 
            IConfiguration configuration,
            ILogger<RefreshTokenService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(string userId, string token, string ipAddress, string userAgent)
        {
            var expiryDays = int.Parse(_configuration.GetSection("Jwt")["RefreshTokenExpiryDays"] ?? "30");

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                UserAgent = userAgent
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created refresh token for user {UserId} from IP {IpAddress}", userId, ipAddress);

            return refreshToken;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string token, string userId)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && rt.UserId == userId);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found for user {UserId}", userId);
                return false;
            }

            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token is revoked for user {UserId}", userId);
                return false;
            }

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user {UserId}", userId);
                return false;
            }

            return true;
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Revoked refresh token for user {UserId}", refreshToken.UserId);
            }
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Revoked all refresh tokens for user {UserId}", userId);
        }

        public async Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
        }
    }
}
