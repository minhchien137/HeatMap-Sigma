using System;
using System.ComponentModel.DataAnnotations;

namespace HeatmapSystem.Models
{

    // Theo dõi các lần đăng nhập thất bại để chống brute force

    public class LoginAttempt
    {
        [Key]
        public int Id { get; set; }


        // Tài khoản thử đăng nhập
        [Required]
        [StringLength(50)]
        public string SVNCode { get; set; }


        // IP address
        [Required]
        [StringLength(50)]
        public string IpAddress { get; set; }


        // Thời gian thử
        public DateTime AttemptTime { get; set; }


        /// Thành công hay thất bại
        public bool IsSuccess { get; set; }

        /// Lý do thất bại
        [StringLength(200)]
        public string FailureReason { get; set; }
    }
}
