using System;
using System.Linq;
using HeatmapSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("DangNhap")]
        public IActionResult DangNhap()
        {
            if (HttpContext.Session.GetString("SVNCode") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost("DangNhap")]
        public async Task<IActionResult> DangNhap(string TaiKhoan, string Password, bool RememberMe = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(Password))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Bước 1: Kiểm tra tài khoản có tồn tại không
                var user = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == TaiKhoan);

                if (user == null)
                {
                    // Ghi log - Tài khoản không tồn tại
                    var failLog = new SVN_Logs
                    {
                        SVNCode = TaiKhoan,
                        TimeAccess = DateTime.Now,
                        ActionType = "Login",
                        Description = "Đăng nhập thất bại - Tài khoản không tồn tại"
                    };
                    _context.SVN_Logs.Add(failLog);
                    await _context.SaveChangesAsync();

                    TempData["Error"] = "Không tồn tại tài khoản này!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Bước 2: Tài khoản tồn tại, kiểm tra mật khẩu
                if (user.Password != Password)
                {
                    // Ghi log - Sai mật khẩu
                    var failLog = new SVN_Logs
                    {
                        SVNCode = TaiKhoan,
                        TimeAccess = DateTime.Now,
                        ActionType = "Login",
                        Description = "Đăng nhập thất bại - Sai mật khẩu"
                    };
                    _context.SVN_Logs.Add(failLog);
                    await _context.SaveChangesAsync();

                    TempData["Error"] = "Tài khoản hoặc mật khẩu không chính xác!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Đăng nhập thành công
                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("SVNCode", user.SVNCode);

                // Ghi log đăng nhập thành công
                var successLog = new SVN_Logs
                {
                    SVNCode = user.SVNCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "Login",
                    Description = "Đăng nhập thành công"
                };
                _context.SVN_Logs.Add(successLog);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                ViewBag.TaiKhoan = TaiKhoan;
                return View();
            }
        }


        [HttpGet("DangKy")]
        public IActionResult DangKy()
        {
            if (HttpContext.Session.GetString("SVNCode") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost("DangKy")]
        public async Task<IActionResult> DangKy(string TaiKhoan, string Password, string ConfirmPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    return View();
                }

                if (Password != ConfirmPassword)
                {
                    TempData["Error"] = "Mật khẩu không khớp!";
                    return View();
                }

                // Kiểm tra tài khoản đã tồn tại
                var existingUser = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == TaiKhoan);

                if (existingUser != null)
                {
                    TempData["Error"] = "Tài khoản đã tồn tại!";
                    return View();
                }

                // Tạo user mới - LƯU MẬT KHẨU TRỰC TIẾP (không mã hóa)
                var newUser = new SVN_User
                {
                    SVNCode = TaiKhoan,
                    Password = Password, // Lưu mật khẩu gốc
                    CreateDate = DateTime.Now
                };

                _context.SVN_User.Add(newUser);
                await _context.SaveChangesAsync();

                // Ghi log đăng ký thành công
                var registerLog = new SVN_Logs
                {
                    SVNCode = TaiKhoan,
                    TimeAccess = DateTime.Now,
                    ActionType = "Register",
                    Description = "Đăng ký tài khoản mới thành công"
                };
                _context.SVN_Logs.Add(registerLog);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return View();
            }
        }

        [HttpGet("DangXuat")]
        public async Task<IActionResult> DangXuat()
        {
            var svnCode = HttpContext.Session.GetString("SVNCode");
            
            if (!string.IsNullOrEmpty(svnCode))
            {
                // Ghi log đăng xuất
                var logoutLog = new SVN_Logs
                {
                    SVNCode = svnCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "Logout",
                    Description = "Đăng xuất khỏi hệ thống"
                };
                _context.SVN_Logs.Add(logoutLog);
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }

        // XÓA HÀM HashPassword - KHÔNG CẦN THIẾT NỮA
    }
}