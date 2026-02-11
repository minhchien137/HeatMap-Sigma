using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    /*
       1.Quản lý Refresh Token cho tính năng "Remember Me"
       2.Thay thế việc lưu password trong cookie 
    */
    public class AuthToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SVNCode { get; set; }

        // Refresh Token - dùng để tạo lại Access Token
        [Required]
        [StringLength(500)]
        public string RefreshToken { get; set; }

  
        // Thời gian tạo token
    
        public DateTime CreatedAt { get; set; }


        // Thời gian hết hạn (30 ngày)

        public DateTime ExpiresAt { get; set; }


        // IP address của thiết bị tạo token
        [StringLength(50)]
        public string IpAddress { get; set; }


        // User-Agent của trình duyệt
        [StringLength(500)]
        public string UserAgent { get; set; }

        // Đã bị thu hồi chưa
        public bool IsRevoked { get; set; }

        // Lý do thu hồi
        [StringLength(200)]
        public string RevokeReason { get; set; }

        // Thời gian thu hồi
        public DateTime? RevokedAt { get; set; }

        // Token đã được sử dụng để làm mới chưa
        public bool IsUsed { get; set; }


        // Thời gian sử dụng
        public DateTime? UsedAt { get; set; }

        // Kiểm tra token còn hợp lệ không
        [NotMapped]
        public bool IsValid => !IsRevoked && !IsUsed && DateTime.Now < ExpiresAt;

        // Kiểm tra IP có khớp không

        public bool IsValidIp(string currentIp) => IpAddress == currentIp;

        /// Kiểm tra User-Agent có khớp không (flexible check)
        public bool IsValidUserAgent(string currentUserAgent)
        {
            if (string.IsNullOrEmpty(UserAgent) || string.IsNullOrEmpty(currentUserAgent))
                return false;

            // Chỉ kiểm tra phần browser chính, bỏ qua version chi tiết
            var storedBrowser = ExtractBrowserName(UserAgent);
            var currentBrowser = ExtractBrowserName(currentUserAgent);

            return storedBrowser == currentBrowser;
        }

        private string ExtractBrowserName(string userAgent)
        {
            if (userAgent.Contains("Chrome")) return "Chrome";
            if (userAgent.Contains("Firefox")) return "Firefox";
            if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
            if (userAgent.Contains("Edge")) return "Edge";
            if (userAgent.Contains("Opera")) return "Opera";
            return "Unknown";
        }
    }
}
