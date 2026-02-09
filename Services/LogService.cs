using HeatmapSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HeatmapSystem.Services
{
    public interface ILogService
    {
        Task LogAction(string svnCode, string actionType, string description);
        Task<List<SVN_Logs>> GetRecentLogs(int count = 100);
        Task<List<SVN_Logs>> GetUserLogs(string svnCode);
    }

    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogService> _logger;

        public LogService(ApplicationDbContext context, ILogger<LogService> logger)
        {
            _context = context;
            _logger = logger;
        }

 
        // Ghi log một hành động vào database
        public async Task LogAction(string svnCode, string actionType, string description)
        {
            try
            {
                // Kiểm tra svnCode
                if (string.IsNullOrEmpty(svnCode))
                {
                    _logger.LogWarning("⚠️ LogAction called with empty SVNCode. ActionType: {ActionType}, Description: {Description}", 
                        actionType, description);
                    svnCode = "SYSTEM"; // Default value nếu null
                }

                _logger.LogInformation("📝 LogAction START - SVNCode: {SVNCode}, Type: {ActionType}", svnCode, actionType);

                var log = new SVN_Logs
                {
                    SVNCode = svnCode,
                    ActionType = actionType,
                    Description = description,
                    TimeAccess = DateTime.Now
                };

                _context.SVN_Logs.Add(log);
                
                _logger.LogInformation("📝 LogAction - Added to context, about to save...");
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ LogAction SUCCESS - ID: {LogId}, SVNCode: {SVNCode}, Type: {ActionType}", 
                    log.IdLogs, svnCode, actionType);
            }
            catch (Exception ex)
            {
                // Log error với đầy đủ thông tin
                _logger.LogError(ex, 
                    "❌ ERROR in LogAction - SVNCode: {SVNCode}, ActionType: {ActionType}, Description: {Description}", 
                    svnCode ?? "NULL", actionType, description);
                
                // Log InnerException nếu có
                if (ex.InnerException != null)
                {
                    _logger.LogError("❌ InnerException: {InnerMessage}", ex.InnerException.Message);
                }
            }
        }

        // Lấy danh sách log gần nhất
        public async Task<List<SVN_Logs>> GetRecentLogs(int count = 100)
        {
            return await _context.SVN_Logs
                .OrderByDescending(l => l.TimeAccess)
                .Take(count)
                .ToListAsync();
        }

  
        // Lấy log của một user cụ thể
        public async Task<List<SVN_Logs>> GetUserLogs(string svnCode)
        {
            return await _context.SVN_Logs
                .Where(l => l.SVNCode == svnCode)
                .OrderByDescending(l => l.TimeAccess)
                .ToListAsync();
        }
    }

    // Các loại hành động để ghi log
    public static class LogActionTypes
    {
        // Đối với Account
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string Register = "Register";
        public const string ChangePassword = "ChangePassword";
        
        // Đối với dữ liệu
        public const string ImportData = "ImportData";
        public const string UpdateData = "UpdateData";
        public const string DeleteData = "DeleteData";
        
        // Đối với cài đặt
        public const string UpdateSettings = "UpdateSettings";
        public const string UpdateProfile = "UpdateProfile";
        
        // Đối với báo cáo
        public const string ViewReport = "ViewReport";
        public const string ExportReport = "ExportReportExcel";
        public const string ExportHistoryExcel = "ExportHistoryExcel";

        public const string ExportStaffExcel = "ExportStaffExcel";
        
        // Đối với nhân viên
        public const string AddStaff = "AddStaff";
        public const string UpdateStaff = "UpdateStaff";
        public const string DeleteStaff = "DeleteStaff";
    }
}