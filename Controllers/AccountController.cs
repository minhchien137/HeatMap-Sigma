using System;
using System.Linq;
using HeatmapSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

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
        public async Task<IActionResult> DangNhap()
        {
            // THAY ĐỔI: Kiểm tra session trước
            var svnCode = HttpContext.Session.GetString("SVNCode");
            
            // Nếu đã có session, redirect về Home
            if (!string.IsNullOrEmpty(svnCode))
            {
                return RedirectToAction("Home", "Heatmap");
            }

            // THÊM: Tự động đăng nhập từ cookie "RememberMe"
            if (Request.Cookies.ContainsKey("RememberMe"))
            {
                var rememberCookie = Request.Cookies["RememberMe"];
                if (!string.IsNullOrEmpty(rememberCookie))
                {
                    try
                    {
                        var parts = rememberCookie.Split('|');
                        if (parts.Length == 2)
                        {
                            var taiKhoan = parts[0];
                            var passwordHash = parts[1];
                            
                            // Tìm user trong database
                            var user = await _context.SVN_User
                                .FirstOrDefaultAsync(u => u.SVNCode == taiKhoan);
                            
                            // Kiểm tra password hash khớp không
                            if (user != null && ComputeHash(user.Password) == passwordHash)
                            {
                                // Tự động đăng nhập
                                user.LastLogin = DateTime.Now;
                                await _context.SaveChangesAsync();
                                
                                HttpContext.Session.SetString("SVNCode", user.SVNCode);
                                
                                // Ghi log tự động đăng nhập
                                var autoLoginLog = new SVN_Logs
                                {
                                    SVNCode = user.SVNCode,
                                    TimeAccess = DateTime.Now,
                                    ActionType = "AutoLogin",
                                    Description = "Tự động đăng nhập từ cookie RememberMe"
                                };
                                _context.SVN_Logs.Add(autoLoginLog);
                                await _context.SaveChangesAsync();
                                
                                return RedirectToAction("Home", "Heatmap");
                            }
                            else
                            {
                                // Cookie không hợp lệ, xóa đi
                                Response.Cookies.Delete("RememberMe");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi tự động đăng nhập từ cookie");
                        Response.Cookies.Delete("RememberMe");
                    }
                }
            }

            // Nếu có cookie, điền sẵn thông tin vào form
            if (Request.Cookies.ContainsKey("RememberMe"))
            {
                var rememberCookie = Request.Cookies["RememberMe"];
                if (!string.IsNullOrEmpty(rememberCookie))
                {
                    var parts = rememberCookie.Split('|');
                    if (parts.Length == 2)
                    {
                        ViewBag.TaiKhoan = parts[0];
                        ViewBag.RememberMe = true;
                    }
                }
            }

            return View();
        }

        [HttpPost("DangNhap")]
        public async Task<IActionResult> DangNhap(string TaiKhoan, string Password, string CaptchaInput, string CaptchaCode, bool RememberMe = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(Password))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Kiểm tra CAPTCHA
                if (string.IsNullOrWhiteSpace(CaptchaInput) || string.IsNullOrWhiteSpace(CaptchaCode))
                {
                    TempData["Error"] = "Vui lòng nhập mã xác nhận!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                if (CaptchaInput.ToUpper() != CaptchaCode.ToUpper())
                {
                    TempData["Error"] = "Mã xác nhận không đúng!";
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

                // THAY ĐỔI: Xử lý Remember Me - lưu password hash thay vì plain text
                if (RememberMe)
                {
                    // Tạo hash của mật khẩu để lưu vào cookie (an toàn hơn)
                    var passwordHash = ComputeHash(Password);
                    
                    // Lưu cookie với thời hạn 30 ngày
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = true, // Chỉ gửi qua HTTPS
                        SameSite = SameSiteMode.Strict
                    };
                    Response.Cookies.Append("RememberMe", $"{TaiKhoan}|{passwordHash}", cookieOptions);
                }
                else
                {
                    // Xóa cookie nếu không chọn Remember Me
                    Response.Cookies.Delete("RememberMe");
                }

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

                return RedirectToAction("Home", "Heatmap");
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
        public async Task<IActionResult> DangKy(string TaiKhoan, string Password, string ConfirmPassword, string CaptchaInput, string CaptchaCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Kiểm tra CAPTCHA
                if (string.IsNullOrWhiteSpace(CaptchaInput) || string.IsNullOrWhiteSpace(CaptchaCode))
                {
                    TempData["Error"] = "Vui lòng nhập mã xác nhận!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                if (CaptchaInput.ToUpper() != CaptchaCode.ToUpper())
                {
                    TempData["Error"] = "Mã xác nhận không đúng!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                if (Password != ConfirmPassword)
                {
                    TempData["Error"] = "Mật khẩu không khớp!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Kiểm tra tài khoản đã tồn tại
                var existingUser = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == TaiKhoan);

                if (existingUser != null)
                {
                    TempData["Error"] = "Tài khoản đã tồn tại!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Tạo user mới
                var newUser = new SVN_User
                {
                    SVNCode = TaiKhoan,
                    Password = Password,
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
                ViewBag.TaiKhoan = TaiKhoan;
                return View();
            }
        }


         [HttpGet("Account")]
        public async Task<IActionResult> Account()
        {
            var svnCode = HttpContext.Session.GetString("SVNCode");
            
            if (string.IsNullOrEmpty(svnCode))
            {
                return RedirectToAction("DangNhap");
            }

            var user = await _context.SVN_User.FirstOrDefaultAsync(u => u.SVNCode == svnCode);
            
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("DangNhap");
            }

            return View(user);
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            try
            {
                var svnCode = HttpContext.Session.GetString("SVNCode");
                
                if (string.IsNullOrEmpty(svnCode))
                {
                    return RedirectToAction("DangNhap");
                }

                if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmNewPassword))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    return RedirectToAction("Account");
                }

                if (NewPassword != ConfirmNewPassword)
                {
                    TempData["Error"] = "Mật khẩu mới không khớp!";
                    return RedirectToAction("Account");
                }

                if (NewPassword.Length <= 6)
                {
                    TempData["Error"] = "Mật khẩu mới phải lớn hơn 6 ký tự!";
                    return RedirectToAction("Account");
                }

                if (!Regex.IsMatch(NewPassword, "[a-zA-Z]") || !Regex.IsMatch(NewPassword, "[0-9]"))
                {
                    TempData["Error"] = "Mật khẩu phải bao gồm cả chữ và số!";
                    return RedirectToAction("Account");
                }

                if (!Regex.IsMatch(NewPassword, @"[^\w\s]"))
                {
                    TempData["Error"] = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt!";
                    return RedirectToAction("Account");
                }

                var user = await _context.SVN_User.FirstOrDefaultAsync(u => u.SVNCode == svnCode);
                
                if (user == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("DangNhap");
                }

                if (user.Password != CurrentPassword)
                {
                    TempData["Error"] = "Mật khẩu hiện tại không chính xác!";
                    
                    // Ghi log thất bại
                    var failLog = new SVN_Logs
                    {
                        SVNCode = svnCode,
                        TimeAccess = DateTime.Now,
                        ActionType = "ChangePassword",
                        Description = "Đổi mật khẩu thất bại - Sai mật khẩu hiện tại"
                    };
                    _context.SVN_Logs.Add(failLog);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction("Account");
                }

                // Đổi mật khẩu
                user.Password = NewPassword;
                await _context.SaveChangesAsync();
                
                // THÊM: Cập nhật cookie RememberMe nếu có
                if (Request.Cookies.ContainsKey("RememberMe"))
                {
                    var passwordHash = ComputeHash(NewPassword);
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    };
                    Response.Cookies.Append("RememberMe", $"{svnCode}|{passwordHash}", cookieOptions);
                }

                // Ghi log thành công
                var successLog = new SVN_Logs
                {
                    SVNCode = svnCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "ChangePassword",
                    Description = "Đổi mật khẩu thành công"
                };
                _context.SVN_Logs.Add(successLog);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return RedirectToAction("Account");
            }
        }

        [HttpGet("DangXuat")]
        public async Task<IActionResult> DangXuat()
        {
            var svnCode = HttpContext.Session.GetString("SVNCode");
            
            if (!string.IsNullOrEmpty(svnCode))
            {
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
            
            // THÊM: Có thể giữ cookie RememberMe để lần sau tự động đăng nhập
            // Hoặc xóa nếu muốn đăng xuất hoàn toàn
            // Response.Cookies.Delete("RememberMe");
            
            return RedirectToAction("DangNhap");
        }
        
        // THÊM: Hàm tạo hash cho password
        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}