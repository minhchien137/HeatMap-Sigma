using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HeatmapSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HeatmapSystem.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        Task<string> GenerateRefreshToken(string svnCode, string ipAddress, string userAgent);
        Task<bool> ValidateRefreshToken(string token, string ipAddress, string userAgent);
        Task RevokeRefreshToken(string token, string reason);
        Task RevokeAllUserTokens(string svnCode, string reason);
        Task<bool> IsAccountLocked(string svnCode, string ipAddress);
        Task RecordLoginAttempt(string svnCode, string ipAddress, bool isSuccess, string failureReason = null);
        Task CleanupExpiredTokens();
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        
        // Cấu hình bảo mật
        private const int MaxFailedAttempts = 5; // Tối đa 5 lần thất bại
        private const int LockoutMinutes = 15;   // Khóa 15 phút
        private const int RefreshTokenExpiryDays = 30; // Token hết hạn sau 30 ngày

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

    
        // Hash mật khẩu bằng BCrypt
 
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }


        // Kiểm tra mật khẩu với hash
        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }


        // Tạo Refresh Token mới

        public async Task<string> GenerateRefreshToken(string svnCode, string ipAddress, string userAgent)
        {
            // Tạo random token
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var token = Convert.ToBase64String(randomBytes);

            // Lưu vào database
            var authToken = new AuthToken
            {
                SVNCode = svnCode,
                RefreshToken = token,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(RefreshTokenExpiryDays),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsRevoked = false,
                IsUsed = false
            };

            _context.AuthTokens.Add(authToken);
            await _context.SaveChangesAsync();

            return token;
        }


        // Validate Refresh Token

        public async Task<bool> ValidateRefreshToken(string token, string ipAddress, string userAgent)
        {
            var authToken = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == token);

            if (authToken == null)
                return false;

            // Kiểm tra token còn hợp lệ
            if (!authToken.IsValid)
            {
                _logger.LogWarning($"Invalid token used for {authToken.SVNCode}");
                return false;
            }

            // Kiểm tra IP
            if (!authToken.IsValidIp(ipAddress))
            {
                _logger.LogWarning($"IP mismatch for token of {authToken.SVNCode}. Expected: {authToken.IpAddress}, Got: {ipAddress}");
                
                // OPTION: Có thể cho phép IP khác nhau nếu cùng User-Agent
                // return false;
            }

            // Kiểm tra User-Agent
            if (!authToken.IsValidUserAgent(userAgent))
            {
                _logger.LogWarning($"User-Agent mismatch for token of {authToken.SVNCode}");
                // OPTION: Tùy chọn có chặn hay không
                // return false;
            }

            return true;
        }


        // Thu hồi Refresh Token

        public async Task RevokeRefreshToken(string token, string reason)
        {
            var authToken = await _context.AuthTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == token);

            if (authToken != null && !authToken.IsRevoked)
            {
                authToken.IsRevoked = true;
                authToken.RevokeReason = reason;
                authToken.RevokedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Token revoked for {authToken.SVNCode}: {reason}");
            }
        }

 
        /// Thu hồi tất cả token của user

        public async Task RevokeAllUserTokens(string svnCode, string reason)
        {
            var tokens = await _context.AuthTokens
                .Where(t => t.SVNCode == svnCode && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokeReason = reason;
                token.RevokedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"All tokens revoked for {svnCode}: {reason}");
        }

        // Kiểm tra tài khoản có bị khóa không

        public async Task<bool> IsAccountLocked(string svnCode, string ipAddress)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-LockoutMinutes);

            var recentFailures = await _context.LoginAttempts
                .Where(a => a.SVNCode == svnCode 
                         && a.IpAddress == ipAddress 
                         && !a.IsSuccess 
                         && a.AttemptTime > cutoffTime)
                .CountAsync();

            return recentFailures >= MaxFailedAttempts;
        }


        // Ghi lại lần đăng nhập

        public async Task RecordLoginAttempt(string svnCode, string ipAddress, bool isSuccess, string failureReason = null)
        {
            var attempt = new LoginAttempt
            {
                SVNCode = svnCode,
                IpAddress = ipAddress,
                AttemptTime = DateTime.Now,
                IsSuccess = isSuccess,
                FailureReason = failureReason
            };

            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }


        // Dọn dẹp token hết hạn (chạy định kỳ)

        public async Task CleanupExpiredTokens()
        {
            var expiredTokens = await _context.AuthTokens
                .Where(t => t.ExpiresAt < DateTime.Now || t.IsUsed)
                .ToListAsync();

            _context.AuthTokens.RemoveRange(expiredTokens);

            var oldAttempts = await _context.LoginAttempts
                .Where(a => a.AttemptTime < DateTime.Now.AddDays(-30))
                .ToListAsync();

            _context.LoginAttempts.RemoveRange(oldAttempts);

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Cleaned up {expiredTokens.Count} expired tokens and {oldAttempts.Count} old login attempts");
        }
    }
}