using System;
using System.Linq;
using HeatmapSystem.Models;
using HeatmapSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IAuthService _authService;

        public AccountController(
            ApplicationDbContext context, 
            ILogger<AccountController> logger,
            IAuthService authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

        [HttpGet("DangNhap")]
        public async Task<IActionResult> DangNhap()
        {
            // Kiểm tra session trước
            var svnCode = HttpContext.Session.GetString("SVNCode");
            
            if (!string.IsNullOrEmpty(svnCode))
            {
                // Kiểm tra IsAdmin để redirect
                var isAdminStr = HttpContext.Session.GetString("IsAdmin");
                bool isAdmin = isAdminStr == "true";
                
                if (isAdmin) // Admin
                {
                    return RedirectToAction("Users", "Admin");
                }
                else // User
                {
                    return RedirectToAction("Home", "Heatmap");
                }
            }

            // ✅ Tự động đăng nhập từ Refresh Token
            if (Request.Cookies.ContainsKey("RefreshToken"))
            {
                var refreshToken = Request.Cookies["RefreshToken"];
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    try
                    {
                        var ipAddress = GetClientIpAddress();
                        var userAgent = Request.Headers["User-Agent"].ToString();
                        
                        // Validate refresh token
                        var isValid = await _authService.ValidateRefreshToken(refreshToken, ipAddress, userAgent);
                        
                        if (isValid)
                        {
                            // Lấy thông tin user từ token
                            var authToken = await _context.AuthTokens
                                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken 
                                                       && !t.IsRevoked 
                                                       && !t.IsUsed 
                                                       && t.ExpiresAt > DateTime.Now);
                            
                            if (authToken != null)
                            {
                                var user = await _context.SVN_User
                                    .FirstOrDefaultAsync(u => u.SVNCode == authToken.SVNCode);
                                
                                if (user != null)
                                {
                                    // Tự động đăng nhập
                                    user.LastLogin = DateTime.Now;
                                    await _context.SaveChangesAsync();
                                    
                                    HttpContext.Session.SetString("SVNCode", user.SVNCode);
                                    HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString().ToLower());
                                    HttpContext.Session.SetString("Permission", user.Permission ?? "None"); // LƯU PERMISSION VÀO SESSION
                                    
                                    // Ghi log
                                    var autoLoginLog = new SVN_Logs
                                    {
                                        SVNCode = user.SVNCode,
                                        TimeAccess = DateTime.Now,
                                        ActionType = "AutoLogin",
                                        Description = $"Tự động đăng nhập từ Refresh Token (IP: {ipAddress})"
                                    };
                                    _context.SVN_Logs.Add(autoLoginLog);
                                    await _context.SaveChangesAsync();
                                    
                                    // Ghi login attempt thành công
                                    await _authService.RecordLoginAttempt(user.SVNCode, ipAddress, true);
                                    
                                    // REDIRECT THEO IsAdmin
                                    if (user.IsAdmin) // Admin
                                    {
                                        return RedirectToAction("Users", "Admin");
                                    }
                                    else // User
                                    {
                                        return RedirectToAction("Home", "Heatmap");
                                    }
                                }
                            }
                        }
                        
                        // Token không hợp lệ, xóa cookie
                        Response.Cookies.Delete("RefreshToken");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi tự động đăng nhập từ Refresh Token");
                        Response.Cookies.Delete("RefreshToken");
                    }
                }
            }

            return View();
        }

        [HttpPost("DangNhap")]
        public async Task<IActionResult> DangNhap(
            string TaiKhoan, 
            string Password, 
            string CaptchaInput, 
            string CaptchaCode, 
            bool RememberMe = false)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Validation cơ bản
                if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(Password))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // ✅ Kiểm tra tài khoản có bị khóa không (Brute Force Protection)
                var isLocked = await _authService.IsAccountLocked(TaiKhoan, ipAddress);
                if (isLocked)
                {
                    TempData["Error"] = "Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau 5 phút.";
                    ViewBag.TaiKhoan = TaiKhoan;
                    
                    // Ghi log
                    var lockLog = new SVN_Logs
                    {
                        SVNCode = TaiKhoan,
                        TimeAccess = DateTime.Now,
                        ActionType = "Login",
                        Description = $"Đăng nhập bị chặn - Tài khoản bị khóa do quá nhiều lần thất bại từ IP {ipAddress}"
                    };
                    _context.SVN_Logs.Add(lockLog);
                    await _context.SaveChangesAsync();
                    
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
                    await _authService.RecordLoginAttempt(TaiKhoan, ipAddress, false, "Sai CAPTCHA");
                    return View();
                }

                // Kiểm tra tài khoản tồn tại
                var user = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == TaiKhoan);

                if (user == null)
                {
                    // Ghi log thất bại
                    var failLog = new SVN_Logs
                    {
                        SVNCode = TaiKhoan,
                        TimeAccess = DateTime.Now,
                        ActionType = "Login",
                        Description = $"Đăng nhập thất bại - Tài khoản không tồn tại (IP: {ipAddress})"
                    };
                    _context.SVN_Logs.Add(failLog);
                    await _context.SaveChangesAsync();

                    // Ghi login attempt thất bại
                    await _authService.RecordLoginAttempt(TaiKhoan, ipAddress, false, "Tài khoản không tồn tại");

                    TempData["Error"] = "Tài khoản hoặc mật khẩu không chính xác!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // ✅ Verify password với BCrypt
                bool isPasswordValid = _authService.VerifyPassword(Password, user.Password);

                if (!isPasswordValid)
                {
                    // Ghi log thất bại
                    var failLog = new SVN_Logs
                    {
                        SVNCode = TaiKhoan,
                        TimeAccess = DateTime.Now,
                        ActionType = "Login",
                        Description = $"Đăng nhập thất bại - Sai mật khẩu (IP: {ipAddress})"
                    };
                    _context.SVN_Logs.Add(failLog);
                    await _context.SaveChangesAsync();

                    // Ghi login attempt thất bại
                    await _authService.RecordLoginAttempt(TaiKhoan, ipAddress, false, "Sai mật khẩu");

                    TempData["Error"] = "Tài khoản hoặc mật khẩu không chính xác!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Đăng nhập thành công
                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("SVNCode", user.SVNCode);
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString().ToLower());
                HttpContext.Session.SetString("Permission", user.Permission ?? "None"); // LƯU PERMISSION VÀO SESSION

                // Tạo Refresh Token nếu chọn "Remember Me"
                if (RememberMe)
                {
                    var refreshToken = await _authService.GenerateRefreshToken(
                        user.SVNCode, 
                        ipAddress, 
                        userAgent);

                    // Lưu vào cookie (30 ngày)
                    Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.Now.AddDays(30)
                    });
                }

                // Ghi log thành công
                var successLog = new SVN_Logs
                {
                    SVNCode = TaiKhoan,
                    TimeAccess = DateTime.Now,
                    ActionType = "Login",
                    Description = $"Đăng nhập thành công{(RememberMe ? " (Remember Me)" : "")} (IP: {ipAddress})"
                };
                _context.SVN_Logs.Add(successLog);
                await _context.SaveChangesAsync();

                // Ghi login attempt thành công
                await _authService.RecordLoginAttempt(TaiKhoan, ipAddress, true);

                if (user.IsAdmin) 
                {
                    return RedirectToAction("Users", "Admin");
                }
                else 
                {
                    return RedirectToAction("Home", "Heatmap");
                }
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
            return View();
        }

        [HttpPost("DangKy")]
        public async Task<IActionResult> DangKy(
            string TaiKhoan, 
            string Password, 
            string ConfirmPassword, 
            string CaptchaInput, 
            string CaptchaCode)
        {
            try
            {
                // Validation cơ bản
                if (string.IsNullOrWhiteSpace(TaiKhoan) || 
                    string.IsNullOrWhiteSpace(Password) || 
                    string.IsNullOrWhiteSpace(ConfirmPassword))
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

                // Kiểm tra tài khoản đã tồn tại
                var existingUser = await _context.SVN_User
                    .FirstOrDefaultAsync(u => u.SVNCode == TaiKhoan);

                if (existingUser != null)
                {
                    TempData["Error"] = "Tài khoản đã tồn tại!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Validate độ dài mật khẩu
                if (Password.Length < 6)
                {
                    TempData["Error"] = "Mật khẩu phải có ít nhất 6 ký tự!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Validate mật khẩu chứa chữ và số
                if (!Regex.IsMatch(Password, "[a-zA-Z]") || !Regex.IsMatch(Password, "[0-9]"))
                {
                    TempData["Error"] = "Mật khẩu phải bao gồm cả chữ và số!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Validate mật khẩu có ký tự đặc biệt
                if (!Regex.IsMatch(Password, @"[^\w\s]"))
                {
                    TempData["Error"] = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                // Kiểm tra confirm password
                if (Password != ConfirmPassword)
                {
                    TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                    ViewBag.TaiKhoan = TaiKhoan;
                    return View();
                }

                //  Hash password với BCrypt
                var hashedPassword = _authService.HashPassword(Password);

              
                var newUser = new SVN_User
                {
                    SVNCode = TaiKhoan,
                    Password = hashedPassword,
                    CreateDate = DateTime.Now,
                    IsAdmin = false  // MẶC ĐỊNH LÀ USER THƯỜNG
                };

                _context.SVN_User.Add(newUser);
                await _context.SaveChangesAsync();

                // Ghi log
                var ipAddress = GetClientIpAddress();
                var registerLog = new SVN_Logs
                {
                    SVNCode = TaiKhoan,
                    TimeAccess = DateTime.Now,
                    ActionType = "Register",
                    Description = $"Đăng ký tài khoản mới (IP: {ipAddress})"
                };
                _context.SVN_Logs.Add(registerLog);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
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

            var user = await _context.SVN_User
                .FirstOrDefaultAsync(u => u.SVNCode == svnCode);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("DangNhap");
            }

            // Lấy danh sách active refresh tokens
            var activeTokens = await _context.AuthTokens
                .Where(t => t.SVNCode == svnCode 
                         && !t.IsRevoked 
                         && !t.IsUsed 
                         && t.ExpiresAt > DateTime.Now)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveTokens = activeTokens;
            ViewBag.CurrentRefreshToken = Request.Cookies["RefreshToken"];

            return View(user);
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            try
            {
                var svnCode = HttpContext.Session.GetString("SVNCode");
                
                if (string.IsNullOrEmpty(svnCode))
                {
                    return RedirectToAction("DangNhap");
                }

                if (string.IsNullOrWhiteSpace(CurrentPassword) || 
                    string.IsNullOrWhiteSpace(NewPassword) || 
                    string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                    return RedirectToAction("Account");
                }

                if (NewPassword != ConfirmPassword)
                {
                    TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                    return RedirectToAction("Account");
                }

                if (NewPassword.Length < 6)
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

                // ✅ Verify current password với BCrypt
                if (!_authService.VerifyPassword(CurrentPassword, user.Password))
                {
                    TempData["Error"] = "Mật khẩu hiện tại không chính xác!";
                    
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

                // ✅ Hash new password
                user.Password = _authService.HashPassword(NewPassword);
                await _context.SaveChangesAsync();
                
                // ✅ Thu hồi TẤT CẢ refresh token khi đổi mật khẩu (bảo mật)
                await _authService.RevokeAllUserTokens(svnCode, "Đổi mật khẩu");
                Response.Cookies.Delete("RefreshToken");

                // Ghi log
                var successLog = new SVN_Logs
                {
                    SVNCode = svnCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "ChangePassword",
                    Description = "Đổi mật khẩu thành công - Tất cả token đã bị thu hồi"
                };
                _context.SVN_Logs.Add(successLog);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đổi mật khẩu thành công! Tất cả phiên đăng nhập khác đã bị đăng xuất.";
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
                var ipAddress = GetClientIpAddress();
                var logoutLog = new SVN_Logs
                {
                    SVNCode = svnCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "Logout",
                    Description = $"Đăng xuất khỏi hệ thống (IP: {ipAddress})"
                };
                _context.SVN_Logs.Add(logoutLog);
                await _context.SaveChangesAsync();

                // Thu hồi refresh token hiện tại khi đăng xuất
                var refreshToken = Request.Cookies["RefreshToken"];
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _authService.RevokeRefreshToken(refreshToken, "Đăng xuất");
                }
            }

            HttpContext.Session.Clear();
            Response.Cookies.Delete("RefreshToken");
            
            return RedirectToAction("DangNhap");
        }

        //  Thu hồi một refresh token cụ thể
        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken(int tokenId)
        {
            try
            {
                var svnCode = HttpContext.Session.GetString("SVNCode");
                
                if (string.IsNullOrEmpty(svnCode))
                {
                    return RedirectToAction("DangNhap");
                }

                var token = await _context.AuthTokens
                    .FirstOrDefaultAsync(t => t.Id == tokenId && t.SVNCode == svnCode);

                if (token != null)
                {
                    await _authService.RevokeRefreshToken(token.RefreshToken, "Người dùng thu hồi thủ công");
                    
                    // Nếu thu hồi token hiện tại, xóa cookie
                    var currentToken = Request.Cookies["RefreshToken"];
                    if (currentToken == token.RefreshToken)
                    {
                        Response.Cookies.Delete("RefreshToken");
                    }
                    
                    TempData["Success"] = "Đã thu hồi phiên đăng nhập!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy phiên đăng nhập!";
                }

                return RedirectToAction("Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thu hồi token");
                TempData["Error"] = "Có lỗi xảy ra!";
                return RedirectToAction("Account");
            }
        }

        // Thu hồi tất cả token
        [HttpPost("RevokeAllTokens")]
        public async Task<IActionResult> RevokeAllTokens()
        {
            try
            {
                var svnCode = HttpContext.Session.GetString("SVNCode");
                
                if (string.IsNullOrEmpty(svnCode))
                {
                    return RedirectToAction("DangNhap");
                }

                await _authService.RevokeAllUserTokens(svnCode, "Người dùng thu hồi tất cả");
                Response.Cookies.Delete("RefreshToken");

                TempData["Success"] = "Đã đăng xuất tất cả phiên đăng nhập!";
                return RedirectToAction("Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thu hồi tất cả token");
                TempData["Error"] = "Có lỗi xảy ra!";
                return RedirectToAction("Account");
            }
        }


        // Lấy IP address của client
        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            // Kiểm tra X-Forwarded-For header (nếu có proxy/load balancer)
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }
            
            return ipAddress ?? "Unknown";
        }
    }
}
