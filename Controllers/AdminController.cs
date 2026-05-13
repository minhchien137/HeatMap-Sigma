using System;
using System.Linq;
using HeatmapSystem.Models;
using HeatmapSystem.Services;
using HeatmapSystem.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    [AdminOnly] 
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

 
        // Trang quản lý users và logs

        [HttpGet("Users")]
        public async Task<IActionResult> Users(
            string svnCodeFilter = null,
            string actionTypeFilter = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50)
        {
            // TODO: Thêm kiểm tra quyền admin ở đây nếu cần
            // var currentUser = HttpContext.Session.GetString("SVNCode");
            // if (currentUser != "ADMIN_CODE") return Forbid();

            var users = await _context.SVN_User
                .OrderByDescending(u => u.CreateDate)
                .ToListAsync();

            // Lấy logs với điều kiện lọc
            var logsQuery = _context.SVN_Logs.AsQueryable();

            // Filter by SVNCode
            if (!string.IsNullOrWhiteSpace(svnCodeFilter))
            {
                logsQuery = logsQuery.Where(l => l.SVNCode.Contains(svnCodeFilter));
            }

            // Filter by ActionType
            if (!string.IsNullOrWhiteSpace(actionTypeFilter))
            {
                logsQuery = logsQuery.Where(l => l.ActionType == actionTypeFilter);
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.TimeAccess >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var toDateEnd = toDate.Value.Date.AddDays(1).AddTicks(-1);
                logsQuery = logsQuery.Where(l => l.TimeAccess <= toDateEnd);
            }

            // Tổng số logs sau khi filter
            var totalLogs = await logsQuery.CountAsync();

            // Lấy logs theo trang
            var logs = await logsQuery
                .OrderByDescending(l => l.TimeAccess)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy danh sách ActionType unique để dùng cho filter dropdown
            var actionTypes = await _context.SVN_Logs
                .Select(l => l.ActionType)
                .Distinct()
                .Where(a => !string.IsNullOrEmpty(a))
                .OrderBy(a => a)
                .ToListAsync();

            // Pass data to view
            ViewBag.Logs = logs;
            ViewBag.TotalLogs = totalLogs;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
            ViewBag.ActionTypes = actionTypes;
            
            // Preserve filter values
            ViewBag.SvnCodeFilter = svnCodeFilter;
            ViewBag.ActionTypeFilter = actionTypeFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

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


        // Set Permission cho user

        [HttpPost("SetPermission")]
        public async Task<IActionResult> SetPermission(string SVNCode, string Permission)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(SVNCode) || string.IsNullOrWhiteSpace(Permission))
                {
                    TempData["Error"] = "Dữ liệu không hợp lệ!";
                    return RedirectToAction("Users");
                }

                // Validate Permission value
                var validPermissions = new[] { "None", "Read", "Update" };
                if (!validPermissions.Contains(Permission))
                {
                    TempData["Error"] = "Quyền không hợp lệ!";
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

                // Không cho phép set permission cho Admin
                if (user.IsAdmin)
                {
                    TempData["Error"] = "Không thể thay đổi quyền của Admin!";
                    return RedirectToAction("Users");
                }

                var oldPermission = user.Permission;
                user.Permission = Permission;
                await _context.SaveChangesAsync();

                // Ghi log
                var adminUser = HttpContext.Session.GetString("SVNCode") ?? "ADMIN";
                var permissionLog = new SVN_Logs
                {
                    SVNCode = SVNCode,
                    TimeAccess = DateTime.Now,
                    ActionType = "AdminSetPermission",
                    Description = $"Admin [{adminUser}] đã thay đổi quyền từ [{oldPermission}] sang [{Permission}]"
                };
                _context.SVN_Logs.Add(permissionLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin set permission for user {SVNCode}: {oldPermission} -> {Permission}");

                TempData["Success"] = $"✅ Đã cập nhật quyền cho [{SVNCode}] thành [{Permission}]";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi set permission");
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại!";
                return RedirectToAction("Users");
            }
        }

        // ── DTOs ─────────────────────────────────────────────────────────────────────

        public class WorkConfigDto
        {
            public int Id { get; set; }
            public string SMStaff { get; set; }
            public decimal WorkHoursPerDay { get; set; }
            public DateTime EffectiveFrom { get; set; }
            public DateTime? EffectiveTo { get; set; }
            public string UpdatedBy { get; set; }
            public DateTime UpdatedAt { get; set; }
            public bool IsActive { get; set; }
        }

        public class SaveWorkConfigRequest
        {
            public int? Id { get; set; }
            public string SMStaff { get; set; }
            public decimal WorkHoursPerDay { get; set; }
            public string EffectiveFrom { get; set; }
            public string EffectiveTo { get; set; }
        }

        // ── GET: list with filter + pagination ───────────────────────────────────────

        [HttpGet("GetWorkConfigs")]
        public async Task<IActionResult> GetWorkConfigs(
            string smStaff = "",
            string status  = "",
            int page       = 1,
            int pageSize   = 20)
        {
            var query = _context.personnel_employee_workconfig.AsQueryable();

            if (!string.IsNullOrWhiteSpace(smStaff))
                query = query.Where(w => w.SMStaff.Contains(smStaff.Trim().ToUpper()));

            var today = DateTime.Today;
            if (status == "active")
                query = query.Where(w => w.EffectiveTo == null || w.EffectiveTo.Value.Date >= today);
            else if (status == "expired")
                query = query.Where(w => w.EffectiveTo != null && w.EffectiveTo.Value.Date < today);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(w => w.EffectiveFrom)
                .ThenBy(w => w.SMStaff)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WorkConfigDto
                {
                    Id              = w.Id,
                    SMStaff         = w.SMStaff,
                    WorkHoursPerDay = w.WorkHoursPerDay,
                    EffectiveFrom   = w.EffectiveFrom,
                    EffectiveTo     = w.EffectiveTo,
                    UpdatedBy       = w.UpdatedBy ?? "",
                    UpdatedAt       = w.UpdatedAt,
                    IsActive        = w.EffectiveTo == null || w.EffectiveTo.Value.Date >= today
                })
                .ToListAsync();

            return Json(new
            {
                data       = items,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        // ── POST: create or update ────────────────────────────────────────────────────

        [HttpPost("SaveWorkConfig")]
        public async Task<IActionResult> SaveWorkConfig([FromBody] SaveWorkConfigRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.SMStaff))
                    return Json(new { success = false, message = "SMStaff không được để trống" });

                if (req.WorkHoursPerDay <= 0 || req.WorkHoursPerDay > 24)
                    return Json(new { success = false, message = "Số giờ/ngày phải trong khoảng 0–24" });

                if (!DateTime.TryParse(req.EffectiveFrom, out DateTime from))
                    return Json(new { success = false, message = "Ngày hiệu lực không hợp lệ" });

                DateTime? to = null;
                if (!string.IsNullOrWhiteSpace(req.EffectiveTo)
                    && DateTime.TryParse(req.EffectiveTo, out DateTime toDate))
                    to = toDate;

                if (to.HasValue && to.Value.Date < from.Date)
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu" });

                var admin = HttpContext.Session.GetString("SVNCode") ?? "ADMIN";

                if (req.Id.HasValue && req.Id.Value > 0)
                {
                    var existing = await _context.personnel_employee_workconfig.FindAsync(req.Id.Value);
                    if (existing == null) return Json(new { success = false, message = "Không tìm thấy bản ghi" });

                    existing.SMStaff         = req.SMStaff.Trim().ToUpper();
                    existing.WorkHoursPerDay = req.WorkHoursPerDay;
                    existing.EffectiveFrom   = from;
                    existing.EffectiveTo     = to;
                    existing.UpdatedBy       = admin;
                    existing.UpdatedAt       = DateTime.Now;
                }
                else
                {
                    _context.personnel_employee_workconfig.Add(new HeatmapSystem.Models.personnel_employee_workconfig
                    {
                        SMStaff         = req.SMStaff.Trim().ToUpper(),
                        WorkHoursPerDay = req.WorkHoursPerDay,
                        EffectiveFrom   = from,
                        EffectiveTo     = to,
                        UpdatedBy       = admin,
                        UpdatedAt       = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving work config");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── POST: delete ─────────────────────────────────────────────────────────────

        [HttpPost("DeleteWorkConfig")]
        public async Task<IActionResult> DeleteWorkConfig([FromBody] int id)
        {
            try
            {
                var item = await _context.personnel_employee_workconfig.FindAsync(id);
                if (item == null) return Json(new { success = false, message = "Không tìm thấy bản ghi" });

                _context.personnel_employee_workconfig.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work config");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── GET: export Excel ─────────────────────────────────────────────────────────

        [HttpGet("ExportWorkConfigs")]
        public async Task<IActionResult> ExportWorkConfigs(string smStaff = "", string status = "")
        {
            try
            {
                var query = _context.personnel_employee_workconfig.AsQueryable();
                if (!string.IsNullOrWhiteSpace(smStaff))
                    query = query.Where(w => w.SMStaff.Contains(smStaff.Trim().ToUpper()));
                var today = DateTime.Today;
                if (status == "active")
                    query = query.Where(w => w.EffectiveTo == null || w.EffectiveTo.Value.Date >= today);
                else if (status == "expired")
                    query = query.Where(w => w.EffectiveTo != null && w.EffectiveTo.Value.Date < today);

                var data = await query
                    .OrderByDescending(w => w.EffectiveFrom)
                    .ThenBy(w => w.SMStaff)
                    .ToListAsync();

                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using var package = new OfficeOpenXml.ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("WorkConfig");

                // Header row
                string[] headers = { "SM Staff", "Giờ/Ngày", "Hiệu lực từ", "Hiệu lực đến", "Trạng thái", "Cập nhật bởi", "Cập nhật lúc" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                    ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 42, 58));
                    ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    ws.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // Data rows
                for (int i = 0; i < data.Count; i++)
                {
                    int row = i + 2;
                    var d = data[i];
                    bool active = d.EffectiveTo == null || d.EffectiveTo.Value.Date >= today;
                    ws.Cells[row, 1].Value = d.SMStaff;
                    ws.Cells[row, 2].Value = (double)d.WorkHoursPerDay;
                    ws.Cells[row, 3].Value = d.EffectiveFrom.ToString("dd/MM/yyyy");
                    ws.Cells[row, 4].Value = d.EffectiveTo?.ToString("dd/MM/yyyy") ?? "Không giới hạn";
                    ws.Cells[row, 5].Value = active ? "Còn hiệu lực" : "Hết hiệu lực";
                    ws.Cells[row, 5].Style.Font.Color.SetColor(active
                        ? System.Drawing.Color.FromArgb(22, 101, 52)
                        : System.Drawing.Color.FromArgb(153, 27, 27));
                    ws.Cells[row, 6].Value = d.UpdatedBy;
                    ws.Cells[row, 7].Value = d.UpdatedAt.ToString("dd/MM/yyyy HH:mm");
                }

                for (int i = 1; i <= 7; i++) ws.Column(i).AutoFit();

                using var ms = new System.IO.MemoryStream();
                package.SaveAs(ms);
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"WorkConfig_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting work configs");
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
