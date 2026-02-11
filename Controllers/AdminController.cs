using System;
using System.Linq;
using HeatmapSystem.Models;
using HeatmapSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuthService _authService;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger,
            IAuthService authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

 
        // Trang quản lý users

        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            // TODO: Thêm kiểm tra quyền admin ở đây nếu cần
            // var currentUser = HttpContext.Session.GetString("SVNCode");
            // if (currentUser != "ADMIN_CODE") return Forbid();

            var users = await _context.SVN_User
                .OrderByDescending(u => u.CreateDate)
                .ToListAsync();

            return View("AdminUsers", users);
        }

        // Reset password cho user

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(string SVNCode, string NewPassword)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(SVNCode) || string.IsNullOrWhiteSpace(NewPassword))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    return RedirectToAction("Users");
                }

                // Tìm user
                var user = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == SVNCode);

                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy tài khoản!";
                    return RedirectToAction("Users");
                }

                // Validate password mới
                if (NewPassword.Length < 8)
                {
                    TempData["Error"] = "Mật khẩu phải có ít nhất 8 ký tự!";
                    return RedirectToAction("Users");
                }

                // Hash password mới
                var hashedPassword = _authService.HashPassword(NewPassword);
                
                // Cập nhật password
                user.Password = hashedPassword;
                await _context.SaveChangesAsync();

                // Thu hồi TẤT CẢ refresh token của user (force logout)
                await _authService.RevokeAllUserTokens(SVNCode, "Admin reset password");

                // Ghi log
                var adminUser = HttpContext.Session.GetString("SVNCode") ?? "ADMIN";
                var resetLog = new SVN_Logs
                {
                    SVNCode = SVNCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "AdminResetPassword",
                    Description = $"Admin [{adminUser}] đã reset mật khẩu"
                };
                _context.SVN_Logs.Add(resetLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin reset password for user: {SVNCode}");

                TempData["Success"] = $"✅ Đã reset mật khẩu cho [{SVNCode}]. Mật khẩu mới: {NewPassword}";
                TempData["ResetPassword"] = NewPassword; // Lưu để hiển thị
                TempData["ResetSVNCode"] = SVNCode;

                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reset password");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return RedirectToAction("Users");
            }
        }


        // Xem chi tiết user

        [HttpGet("UserDetail/{svnCode}")]
        public async Task<IActionResult> UserDetail(string svnCode)
        {
            var user = await _context.SVN_User
                .FirstOrDefaultAsync(u => u.SVNCode == svnCode);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("Users");
            }

            // Lấy active tokens
            var activeTokens = await _context.AuthTokens
                .Where(t => t.SVNCode == svnCode 
                         && !t.IsRevoked 
                         && !t.IsUsed 
                         && t.ExpiresAt > DateTime.Now)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Lấy login history
            var loginHistory = await _context.LoginAttempts
                .Where(a => a.SVNCode == svnCode)
                .OrderByDescending(a => a.AttemptTime)
                .Take(20)
                .ToListAsync();

            ViewBag.ActiveTokens = activeTokens;
            ViewBag.LoginHistory = loginHistory;

            return View(user);
        }


        // Xóa user (nếu cần)

        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser(string SVNCode)
        {
            try
            {
                var user = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == SVNCode);

                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy tài khoản!";
                    return RedirectToAction("Users");
                }

                // Xóa user
                _context.SVN_User.Remove(user);

                // Xóa tất cả tokens của user
                var userTokens = await _context.AuthTokens
                    .Where(t => t.SVNCode == SVNCode)
                    .ToListAsync();
                _context.AuthTokens.RemoveRange(userTokens);

                // Ghi log
                var adminUser = HttpContext.Session.GetString("SVNCode") ?? "ADMIN";
                var deleteLog = new SVN_Logs
                {
                    SVNCode = SVNCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "AdminDeleteUser",
                    Description = $"Admin [{adminUser}] đã xóa tài khoản"
                };
                _context.SVN_Logs.Add(deleteLog);

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa tài khoản [{SVNCode}]!";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa user");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return RedirectToAction("Users");
            }
        }
    }
}