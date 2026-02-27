using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HeatmapSystem.Models;
using HeatmapSystem.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using HeatmapSystem.Attributes;

namespace HeatmapSystem.Controllers
{
    // DTO class để map dữ liệu từ bảng personnel_department
    public class DepartmentDto
    {
        public int id { get; set; }
        public string dept_code { get; set; }
        public string dept_name { get; set; }
        public bool is_default { get; set; }
        public int? parent_dept_id { get; set; }
        public int? dept_manager_id { get; set; }
        public int company_id { get; set; }
    }

    // DTO class để map dữ liệu từ bảng personnel_employee
    public class EmployeeDto
    {
        public int id { get; set; }
        public string emp_code { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string nickname { get; set; }
        public int department_id { get; set; }
        public short status { get; set; }  // SMALLINT trong database
    }

    // DTO class cho Staff data
    public class StaffDto
    {
        public int id { get; set; }
        public string emp_code { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string full_name { get; set; }
        public string gender { get; set; }
        public DateTime? birthday { get; set; }
        public string city { get; set; }
        public DateTime? hire_date { get; set; }
        public string department { get; set; }
        public short status { get; set; }  // SMALLINT trong database
    }

    public class ReportDataDto
{
    public KpiDto kpis { get; set; }
    public List<TrendDataDto> trendData { get; set; }
    public List<TrendDataDto> monthlyTrendData { get; set; }
    public List<DepartmentDataDto> departmentData { get; set; }
    public List<HeatmapDataDto> heatmapData { get; set; }
    public List<DetailDataDto> detailData { get; set; }
}

public class KpiDto
{
    public decimal totalHours { get; set; }
    public decimal avgUtilization { get; set; }
    public int activeProjects { get; set; }
    public int staffCount { get; set; }
}

public class TrendDataDto
{
    public string label { get; set; }
    public decimal hours { get; set; }
    public decimal utilization { get; set; }
}

public class DepartmentDataDto
{
    public string department { get; set; }
    public decimal hours { get; set; }
}

public class HeatmapDataDto
{
    public string project { get; set; }
    public string week { get; set; }
    public string department { get; set; }
    public decimal hours { get; set; }
    public int staffCount { get; set; }
}

public class DetailDataDto
{
    public string project { get; set; }
    public string department { get; set; }
    public int staffCount { get; set; }
    public decimal totalHours { get; set; }
}

    public class StaffDetailDto
    {
        public string name { get; set; }
        public string svnStaff { get; set; }
        public string department { get; set; }
        public decimal hours { get; set; }
        public int days { get; set; }
    }


    // DTO class cho Customer (distinct từ SVN_Projects)
    public class CustomerDto
    {
        public string IdCustomer { get; set; }
        public string NameCustomer { get; set; }
    }

    [Route("[controller]")]
    public class HeatmapController : Controller
    {
        private readonly ILogger<HeatmapController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ZKBioTimeDbContext _zkContext;
        private readonly IReportService _reportService;
        private readonly ILogService _logService;


        public HeatmapController(ILogger<HeatmapController> logger, ApplicationDbContext context, ZKBioTimeDbContext zkContext, IReportService reportService, ILogService logService)
        {
            _logger = logger;
            _context = context;
            _zkContext = zkContext;
            _reportService = reportService;
            _logService = logService;
        }

        // Helper method để lấy SVNCode từ Session
        private string GetCurrentSVNCode()
        {
            return HttpContext.Session.GetString("SVNCode");
        }

        // Helper method để kiểm tra user đã đăng nhập chưa
        private bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetCurrentSVNCode());
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        [RequireUpdate]
        [HttpGet("Import")]
        public IActionResult Import()
        {
            // Kiểm tra authentication
            if (!IsAuthenticated())
            {
                return RedirectToAction("DangNhap", "Account");
            }

            // Lấy bộ phận từ bảng personnel_department trong zkbiotime database
            var departments = _zkContext.Database
                .SqlQueryRaw<DepartmentDto>(@"
                    SELECT 
                        id, 
                        ISNULL(dept_code, '') as dept_code, 
                        ISNULL(dept_name, '') as dept_name, 
                        is_default, 
                        parent_dept_id, 
                        dept_manager_id, 
                        company_id 
                    FROM personnel_department")
                .ToList();
            ViewBag.Departments = departments;

            // Dự án từ Database
            var projects = _context.SVN_Projects.ToList();
            ViewBag.Projects = projects;

            // Customers (distinct từ SVN_Projects)
            var customers = _context.SVN_Projects
                .Where(p => !string.IsNullOrEmpty(p.NameCustomer))
                .Select(p => p.NameCustomer)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => new CustomerDto { IdCustomer = c, NameCustomer = c })
                .ToList();
            ViewBag.Customers = customers;

            // Project Phase từ Database
            var projectPhases = _context.SVN_ProjectPhase.ToList();
            ViewBag.ProjectPhases = projectPhases;

            return View();
        }

        [HttpGet("GetEmployeesByDepartment")]
        public IActionResult GetEmployeesByDepartment(int departmentId)
        {
            try
            {
                var employees = new List<EmployeeDto>();

                using (var connection = _zkContext.Database.GetDbConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                    SELECT 
                        id, 
                        emp_code, 
                        first_name, 
                        last_name, 
                        nickname, 
                        department_id, 
                        status 
                    FROM personnel_employee 
                    WHERE department_id = @departmentId AND status = 0";

                        var param = command.CreateParameter();
                        param.ParameterName = "@departmentId";
                        param.Value = departmentId;
                        command.Parameters.Add(param);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                employees.Add(new EmployeeDto
                                {
                                    id = Convert.ToInt32(reader[0]),  // Đổi thành Convert.ToInt32
                                    emp_code = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    first_name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    last_name = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    nickname = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    department_id = Convert.ToInt32(reader[5]),  // Đổi thành Convert.ToInt32
                                    status = Convert.ToInt16(reader[6])  // Đổi thành Convert.ToInt32
                                });
                            }
                        }
                    }
                }

                return Json(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employees for department {DepartmentId}", departmentId);
                return Json(new { error = ex.Message });
            }
        }


        [HttpGet("Home")]
        public IActionResult Home()
        {
            // Kiểm tra authentication
            if (!IsAuthenticated())
            {
                return RedirectToAction("DangNhap", "Account");
            }

            return View();
        }

        [HttpGet("Setting")]
        public IActionResult Setting()
        {
            // Kiểm tra authentication
            if (!IsAuthenticated())
            {
                return RedirectToAction("DangNhap", "Account");
            }

            return View();
        }

        // DTO cho request từ client
        public class SaveStaffDetailRequest
        {
            public int EmployeeId { get; set; }
            public int ProjectId { get; set; }
            public string Customer { get; set; }
            public string WorkDate { get; set; }
            public decimal WorkHours { get; set; }
            public string ProjectPhase { get; set; }
            public string Phase { get; set; }
        }

        [RequireUpdate]
        [HttpPost("SaveStaffDetail")]
        public IActionResult SaveStaffDetail([FromBody] SaveStaffDetailRequest request)
        {
            try
            {
                EmployeeDto employee = null;
                DepartmentDto department = null;

                // Sử dụng 1 connection cho cả 2 queries
                using (var connection = _zkContext.Database.GetDbConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    // 1. Lấy thông tin nhân viên từ ZKBioTime
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                id, 
                                emp_code, 
                                first_name, 
                                last_name, 
                                nickname, 
                                department_id, 
                                status 
                            FROM personnel_employee 
                            WHERE id = @employeeId";

                        var param = command.CreateParameter();
                        param.ParameterName = "@employeeId";
                        param.Value = request.EmployeeId;
                        command.Parameters.Add(param);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                employee = new EmployeeDto
                                {
                                    id = Convert.ToInt32(reader["id"]),
                                    emp_code = reader.IsDBNull(reader.GetOrdinal("emp_code")) ? "" : reader.GetString(reader.GetOrdinal("emp_code")),
                                    first_name = reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString(reader.GetOrdinal("first_name")),
                                    last_name = reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString(reader.GetOrdinal("last_name")),
                                    nickname = reader.IsDBNull(reader.GetOrdinal("nickname")) ? "" : reader.GetString(reader.GetOrdinal("nickname")),
                                    department_id = Convert.ToInt32(reader["department_id"]),
                                    status = Convert.ToInt16(reader["status"])  // SMALLINT
                                };
                            }
                        }
                    }

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy nhân viên" });
                    }

                    // 2. Lấy tên bộ phận
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                id, 
                                dept_code, 
                                dept_name, 
                                is_default, 
                                parent_dept_id, 
                                dept_manager_id, 
                                company_id 
                            FROM personnel_department 
                            WHERE id = @departmentId";

                        var param = command.CreateParameter();
                        param.ParameterName = "@departmentId";
                        param.Value = employee.department_id;
                        command.Parameters.Add(param);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                department = new DepartmentDto
                                {
                                    id = Convert.ToInt32(reader["id"]),
                                    dept_code = reader.IsDBNull(reader.GetOrdinal("dept_code")) ? "" : reader.GetString(reader.GetOrdinal("dept_code")),
                                    dept_name = reader.IsDBNull(reader.GetOrdinal("dept_name")) ? "" : reader.GetString(reader.GetOrdinal("dept_name")),
                                    is_default = Convert.ToBoolean(reader["is_default"]),
                                    parent_dept_id = reader.IsDBNull(reader.GetOrdinal("parent_dept_id")) ? (int?)null : Convert.ToInt32(reader["parent_dept_id"]),
                                    dept_manager_id = reader.IsDBNull(reader.GetOrdinal("dept_manager_id")) ? (int?)null : Convert.ToInt32(reader["dept_manager_id"]),
                                    company_id = Convert.ToInt32(reader["company_id"])
                                };
                            }
                        }
                    }
                }

                // 3. Lấy tên dự án
                var project = _context.SVN_Projects.Find(request.ProjectId);
                if (project == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy dự án" });
                }

                // 4. Parse WorkDate
                DateTime workDate;
                if (!DateTime.TryParse(request.WorkDate, out workDate))
                {
                    return Json(new { success = false, message = "Ngày làm việc không hợp lệ" });
                }

                // 5. Tính tuần và năm
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var weekNo = culture.Calendar.GetWeekOfYear(
                    workDate,
                    System.Globalization.CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday
                );
                var year = workDate.Year;

                // 6. Lấy SVNCode từ Session
                string currentUser = GetCurrentSVNCode();
                if (string.IsNullOrEmpty(currentUser))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });
                }

                // 7. Tạo tên nhân viên (first_name + last_name)
                string fullName = $"{employee.first_name} {employee.last_name}".Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    fullName = employee.nickname ?? employee.emp_code;
                }

                // 8. Kiểm tra xem đã tồn tại bản ghi chưa
                var existing = _context.SVN_StaffDetail
                    .FirstOrDefault(s =>
                        s.SVNStaff == employee.emp_code &&
                        s.WorkDate.Date == workDate.Date &&
                        s.Project == project.NameProject);

                if (existing != null)
                {
                    // Cập nhật bản ghi hiện có
                    existing.WorkHours = request.WorkHours;
                    existing.Customer = request.Customer ?? project.NameCustomer ?? "";
                    existing.ProjectPhase = request.ProjectPhase ?? "";
                    existing.Phase = request.Phase ?? "";
                    existing.CreateDate = DateTime.Now;
                    existing.CreateBy = currentUser;
                }
                else
                {
                    // Tạo mới
                    var staffDetail = new SVN_StaffDetail
                    {
                        SVNStaff = employee.emp_code,
                        NameStaff = fullName,
                        Department = department?.dept_name ?? "N/A",
                        Customer = request.Customer ?? project.NameCustomer ?? "",
                        Project = project.NameProject,
                        ProjectPhase = request.ProjectPhase ?? "",
                        Phase = request.Phase ?? "",
                        WorkDate = workDate,
                        WeekNo = weekNo,
                        Year = year,
                        WorkHours = request.WorkHours,
                        CreateBy = currentUser,
                        CreateDate = DateTime.Now
                    };

                    _context.SVN_StaffDetail.Add(staffDetail);
                }

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Lưu dữ liệu thành công",
                    data = new
                    {
                        employee = fullName,
                        department = department?.dept_name,
                        project = project.NameProject,
                        workDate = workDate.ToString("dd/MM/yyyy"),
                        workHours = request.WorkHours
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving staff detail");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // DTO cho Mode 2: Một người - Nhiều ngày
        public class DayDataRequest
        {
            public string Date { get; set; }
            public int? ProjectId { get; set; }  // Nullable cho trường hợp common project
            public decimal WorkHours { get; set; }
        }

        public class SaveMultipleDaysRequest
        {
            public int EmployeeId { get; set; }
            public int ProjectMode { get; set; }  // 1 = một dự án chung, 2 = dự án riêng
            public int? CommonProjectId { get; set; }  // Dùng khi ProjectMode = 1
            public string Customer { get; set; }
            public string ProjectPhase { get; set; }
            public string Phase { get; set; }
            public List<DayDataRequest> Days { get; set; }
        }


        [RequireUpdate]
        [HttpPost("SaveMultipleDays")]
        public IActionResult SaveMultipleDays([FromBody] SaveMultipleDaysRequest request)
        {
            try
            {
                EmployeeDto employee = null;
                DepartmentDto department = null;

                // 1. Lấy thông tin nhân viên và bộ phận từ ZKBioTime
                using (var connection = _zkContext.Database.GetDbConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    // Lấy thông tin nhân viên
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                id, emp_code, first_name, last_name, 
                                nickname, department_id, status 
                            FROM personnel_employee 
                            WHERE id = @employeeId";

                        var param = command.CreateParameter();
                        param.ParameterName = "@employeeId";
                        param.Value = request.EmployeeId;
                        command.Parameters.Add(param);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                employee = new EmployeeDto
                                {
                                    id = Convert.ToInt32(reader["id"]),
                                    emp_code = reader.IsDBNull(reader.GetOrdinal("emp_code")) ? "" : reader.GetString(reader.GetOrdinal("emp_code")),
                                    first_name = reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString(reader.GetOrdinal("first_name")),
                                    last_name = reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString(reader.GetOrdinal("last_name")),
                                    nickname = reader.IsDBNull(reader.GetOrdinal("nickname")) ? "" : reader.GetString(reader.GetOrdinal("nickname")),
                                    department_id = Convert.ToInt32(reader["department_id"]),
                                    status = Convert.ToInt16(reader["status"])
                                };
                            }
                        }
                    }

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy nhân viên" });
                    }

                    // Lấy tên bộ phận
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT 
                                id, dept_code, dept_name, is_default, 
                                parent_dept_id, dept_manager_id, company_id 
                            FROM personnel_department 
                            WHERE id = @departmentId";

                        var param = command.CreateParameter();
                        param.ParameterName = "@departmentId";
                        param.Value = employee.department_id;
                        command.Parameters.Add(param);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                department = new DepartmentDto
                                {
                                    id = Convert.ToInt32(reader["id"]),
                                    dept_code = reader.IsDBNull(reader.GetOrdinal("dept_code")) ? "" : reader.GetString(reader.GetOrdinal("dept_code")),
                                    dept_name = reader.IsDBNull(reader.GetOrdinal("dept_name")) ? "" : reader.GetString(reader.GetOrdinal("dept_name")),
                                    is_default = Convert.ToBoolean(reader["is_default"]),
                                    parent_dept_id = reader.IsDBNull(reader.GetOrdinal("parent_dept_id")) ? (int?)null : Convert.ToInt32(reader["parent_dept_id"]),
                                    dept_manager_id = reader.IsDBNull(reader.GetOrdinal("dept_manager_id")) ? (int?)null : Convert.ToInt32(reader["dept_manager_id"]),
                                    company_id = Convert.ToInt32(reader["company_id"])
                                };
                            }
                        }
                    }
                }

                // 2. Lấy SVNCode từ Session
                string currentUser = GetCurrentSVNCode();
                if (string.IsNullOrEmpty(currentUser))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });
                }

                // 3. Tạo tên nhân viên
                string fullName = $"{employee.first_name} {employee.last_name}".Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    fullName = employee.nickname ?? employee.emp_code;
                }

                // 4. Lấy common project nếu ProjectMode = 1
                SVN_Projects commonProject = null;
                if (request.ProjectMode == 1)
                {
                    if (!request.CommonProjectId.HasValue)
                    {
                        return Json(new { success = false, message = "Vui lòng chọn dự án" });
                    }
                    commonProject = _context.SVN_Projects.Find(request.CommonProjectId.Value);
                    if (commonProject == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy dự án" });
                    }
                }

                // 5. Lưu từng ngày
                int savedCount = 0;
                int updatedCount = 0;
                var savedDates = new List<string>();

                foreach (var dayData in request.Days)
                {
                    // Parse work date
                    DateTime workDate;
                    if (!DateTime.TryParse(dayData.Date, out workDate))
                    {
                        continue;  // Skip invalid dates
                    }

                    // Xác định project cho ngày này
                    SVN_Projects project;
                    if (request.ProjectMode == 1)
                    {
                        project = commonProject;
                    }
                    else
                    {
                        if (!dayData.ProjectId.HasValue)
                        {
                            continue;  // Skip days without project
                        }
                        project = _context.SVN_Projects.Find(dayData.ProjectId.Value);
                        if (project == null)
                        {
                            continue;  // Skip if project not found
                        }
                    }

                    // Tính tuần và năm
                    var culture = System.Globalization.CultureInfo.CurrentCulture;
                    var weekNo = culture.Calendar.GetWeekOfYear(
                        workDate,
                        System.Globalization.CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday
                    );
                    var year = workDate.Year;

                    // Kiểm tra bản ghi đã tồn tại chưa
                    var existing = _context.SVN_StaffDetail
                        .FirstOrDefault(s =>
                            s.SVNStaff == employee.emp_code &&
                            s.WorkDate.Date == workDate.Date &&
                            s.Project == project.NameProject);

                    if (existing != null)
                    {
                        // Cập nhật
                        existing.WorkHours = dayData.WorkHours;
                        existing.Customer = request.Customer ?? project.NameCustomer ?? "";
                        existing.ProjectPhase = request.ProjectPhase ?? "";
                        existing.Phase = request.Phase ?? "";
                        existing.CreateDate = DateTime.Now;
                        existing.CreateBy = currentUser;
                        updatedCount++;
                    }
                    else
                    {
                        // Tạo mới
                        var staffDetail = new SVN_StaffDetail
                        {
                            SVNStaff = employee.emp_code,
                            NameStaff = fullName,
                            Department = department?.dept_name ?? "N/A",
                            Customer = request.Customer ?? project.NameCustomer ?? "",
                            Project = project.NameProject,
                            ProjectPhase = request.ProjectPhase ?? "",
                            Phase = request.Phase ?? "",
                            WorkDate = workDate,
                            WeekNo = weekNo,
                            Year = year,
                            WorkHours = dayData.WorkHours,
                            CreateBy = currentUser,
                            CreateDate = DateTime.Now
                        };
                        _context.SVN_StaffDetail.Add(staffDetail);
                        savedCount++;
                    }

                    savedDates.Add(workDate.ToString("dd/MM/yyyy"));
                }

                // 6. Lưu tất cả vào database
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Lưu dữ liệu thành công",
                    data = new
                    {
                        employee = fullName,
                        department = department?.dept_name,
                        savedCount = savedCount,
                        updatedCount = updatedCount,
                        totalDays = savedCount + updatedCount,
                        dates = savedDates
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving multiple days data");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [RequireRead]
        [HttpGet("Staff")]
        public IActionResult Staff()
        {
            // Kiểm tra authentication
            if (!IsAuthenticated())
            {
                return RedirectToAction("DangNhap", "Account");
            }

            return View();
        }
        [RequireUpdate]
        [HttpGet("GetStaffData")]
        public IActionResult GetStaffData()
        {
            try
            {
                var staffData = _zkContext.Database
                    .SqlQueryRaw<StaffDto>(@"
                SELECT 
                    e.id,
                    ISNULL(e.emp_code, '') as emp_code,
                    ISNULL(e.first_name, '') as first_name,
                    ISNULL(e.last_name, '') as last_name,
                    ISNULL(e.first_name, '') as full_name,
                    ISNULL(e.gender, '') as gender,
                    e.birthday,
                    ISNULL(e.city, '') as city,
                    e.hire_date,
                    ISNULL(d.dept_name, '') as department,
                    ISNULL(e.status, 0) as status
                FROM personnel_employee e
                LEFT JOIN personnel_department d ON e.department_id = d.id
                ORDER BY e.emp_code")
                    .ToList();

                return Json(staffData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff data");
                return Json(new List<StaffDto>());
            }
        }

        /*------------------- Lịch sự nhập liệu -----------------------*/
        [RequireRead]
        [HttpGet("History")]
        public IActionResult History()
        {
            // Kiểm tra authentication
            if (!IsAuthenticated())
            {
                return RedirectToAction("DangNhap", "Account");
            }

            return View();
        }
        [RequireUpdate]
        [HttpGet("GetHistoryData")]
        public IActionResult GetHistoryData()
        {
            try
            {
                // Kiểm tra authentication
                if (!IsAuthenticated())
                {
                    return Json(new { error = "Unauthorized" });
                }

                // Lấy dữ liệu từ bảng SVN_StaffDetail
                var data = _context.SVN_StaffDetail
                    .OrderByDescending(s => s.WorkDate)
                    .ThenByDescending(s => s.CreateDate)
                    .Select(s => new
                    {
                        staffDetailId = s.StaffDetailId,
                        svnStaff = s.SVNStaff,
                        nameStaff = s.NameStaff,
                        department = s.Department,
                        customer = s.Customer,
                        project = s.Project,
                        projectPhase = s.ProjectPhase,
                        phase = s.Phase,
                        workDate = s.WorkDate,
                        weekNo = s.WeekNo,
                        year = s.Year,
                        workHours = s.WorkHours,
                        createBy = s.CreateBy,
                        createDate = s.CreateDate
                    })
                    .ToList();

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading history data");
                return Json(new { error = ex.Message });
            }
        }

        [RequireUpdate]
        [HttpDelete("DeleteStaffDetail/{id}")]
        public async Task<IActionResult> DeleteStaffDetail(int id)
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Tìm bản ghi cần xóa
                var staffDetail = await _context.SVN_StaffDetail.FindAsync(id);

                if (staffDetail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bản ghi" });
                }

                // Lấy thông tin để log
                var svnCode = GetCurrentSVNCode();
                var staffInfo = $"{staffDetail.NameStaff} ({staffDetail.SVNStaff})";
                var projectInfo = $"{staffDetail.Project} - Tuần {staffDetail.WeekNo}/{staffDetail.Year}";

                // Xóa bản ghi
                _context.SVN_StaffDetail.Remove(staffDetail);
                await _context.SaveChangesAsync();

        
                await _logService.LogAction(
                    svnCode,
                    LogActionTypes.DeleteData,
                    $"Xóa bản ghi lịch sử: {staffInfo} - {projectInfo} ({staffDetail.WorkHours}h)"
                );

                return Json(new { success = true, message = "Xóa bản ghi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff detail with ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa bản ghi" });
            }
        }

        
        [RequireUpdate]
        [HttpGet("GetStaffDetailById/{id}")]
        public IActionResult GetStaffDetailById(int id)
        {
            try
            {
                // Kiểm tra authentication
                if (!IsAuthenticated())
                {
                    return Json(new { error = "Unauthorized" });
                }

                var record = _context.SVN_StaffDetail.Find(id);
                if (record == null)
                {
                    return Json(new { error = "Không tìm thấy bản ghi" });
                }

                var data = new
                {
                    staffDetailId = record.StaffDetailId,
                    svnStaff = record.SVNStaff,
                    nameStaff = record.NameStaff,
                    department = record.Department,
                    project = record.Project,
                    workDate = record.WorkDate,
                    weekNo = record.WeekNo,
                    year = record.Year,
                    workHours = record.WorkHours,
                    createBy = record.CreateBy,
                    createDate = record.CreateDate
                };

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff detail {Id}", id);
                return Json(new { error = ex.Message });
            }
        }

        // DTO cho việc update
        public class UpdateStaffDetailRequest
        {
            public int StaffDetailId { get; set; }
            public decimal WorkHours { get; set; }
        }

        [RequireUpdate]
        [HttpPut("UpdateStaffDetail")]
        public IActionResult UpdateStaffDetail([FromBody] UpdateStaffDetailRequest request)
        {
            try
            {
                // Kiểm tra authentication
                if (!IsAuthenticated())
                {
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });
                }

                var record = _context.SVN_StaffDetail.Find(request.StaffDetailId);
                if (record == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bản ghi" });
                }

                // Update work hours
                record.WorkHours = request.WorkHours;
                record.CreateDate = DateTime.Now;
                record.CreateBy = GetCurrentSVNCode();

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thành công",
                    data = new
                    {
                        staffDetailId = record.StaffDetailId,
                        workHours = record.WorkHours,
                        createDate = record.CreateDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff detail");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [RequireUpdate]
        [HttpGet("ExportHistoryToExcel")]
        public async Task<IActionResult> ExportHistoryToExcel(
            string department = "",
            string project = "",
            string customer = "",
            string year = "",
            string week = "",
            string search = "")
        {
            try
            {
                // Kiểm tra authentication
                if (!IsAuthenticated())
                {
                    return RedirectToAction("DangNhap", "Account");
                }

                var svnCode = GetCurrentSVNCode();
                // Query dữ liệu với filters
                var query = _context.SVN_StaffDetail.AsQueryable();

                if (!string.IsNullOrEmpty(department))
                {
                    query = query.Where(s => s.Department == department);
                }

                if (!string.IsNullOrEmpty(project))
                {
                    query = query.Where(s => s.Project == project);
                }

                if (!string.IsNullOrEmpty(customer))
                {
                    query = query.Where(s => s.Customer == customer);
                }

                if (!string.IsNullOrEmpty(year))
                {
                    int yearInt = int.Parse(year);
                    query = query.Where(s => s.Year == yearInt);
                }

                if (!string.IsNullOrEmpty(week))
                {
                    int weekInt = int.Parse(week);
                    query = query.Where(s => s.WeekNo == weekInt);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(s =>
                        s.SVNStaff.ToLower().Contains(search) ||
                        s.NameStaff.ToLower().Contains(search));
                }

                var data = query
                    .OrderByDescending(s => s.WorkDate)
                    .ToList();

                // Tạo file Excel bằng EPPlus hoặc NPOI
                // Code này cần cài thêm package, tạm thời return CSV

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("STT,SVN Staff,Tên nhân viên,Bộ phận,Customer,Dự án,Project Phase,Phase,Ngày làm việc,Tuần,Năm,Giờ làm,Người tạo,Ngày tạo");

                int stt = 1;
                foreach (var item in data)
                {
                    csv.AppendLine($"{stt},{item.SVNStaff},{item.NameStaff},{item.Department},{item.Customer},{item.Project}," +
                        $"{item.ProjectPhase},{item.Phase}," +
                        $"{item.WorkDate:dd/MM/yyyy},{item.WeekNo},{item.Year},{item.WorkHours}," +
                        $"{item.CreateBy},{item.CreateDate:dd/MM/yyyy HH:mm}");
                    stt++;
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

                var filterDescription = new List<string>();
                if (!string.IsNullOrEmpty(department))
                {
                    filterDescription.Add($"Bộ phận: {department}");
                }
                if (!string.IsNullOrEmpty(project))
                {
                    filterDescription.Add($"Dự án: {project}");
                }
                if (!string.IsNullOrEmpty(customer))
                {
                    filterDescription.Add($"Customer: {customer}");
                }
                if (!string.IsNullOrEmpty(year))
                {
                    filterDescription.Add($"Năm: {year}");
                }
                if (!string.IsNullOrEmpty(week))
                {
                    filterDescription.Add($"Tuần: {week}");
                }
                if (!string.IsNullOrEmpty(search))
                {
                    filterDescription.Add($"Tìm kiếm: {search}");
                }

                var description = filterDescription.Count > 0
                ? $"Xuất {data.Count} bản ghi lịch sử nhập với bộ lọc: {string.Join(", ", filterDescription)}"
                : $"Xuất tất cả {data.Count} bản ghi lịch sử";

                await _logService.LogAction(svnCode, LogActionTypes.ExportHistoryExcel, description);
                return File(bytes, "text/csv", $"Lich_su_nhap_lieu_{DateTime.Now:yyyyMMddHHmmss}.csv");
                
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting history to Excel");
                return BadRequest(new { error = ex.Message });
            }
        }

        

        #region Report Methods - Using ReportService
        [RequireUpdate]
        [HttpGet("Report")]
        public IActionResult Report()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("DangNhap", "Account");
            }
            return View();
        }

        [HttpGet("GetDepartmentList")]
        public IActionResult GetDepartmentList()
        {
            try
            {
                var departments = _reportService.GetDepartmentList();
                return Json(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading department list");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetProjectList")]
        public IActionResult GetProjectList()
        {
            try
            {
                var projects = _reportService.GetProjectList();
                return Json(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project list");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetPhaseList")]
        public IActionResult GetPhaseList()
        {
            try
            {
                var phases = _reportService.GetPhaseList();
                return Json(phases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading phase list");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetReportData")]
        public IActionResult GetReportData(
            string timeRange = "current_week",
            string year = "",
            string department = "",
            string project = "",
            string phase = "",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return Json(new { error = "Unauthorized" });
                }

                var filter = new ReportFilterDto
                {
                    TimeRange = timeRange,
                    Year = year,
                    Department = department,
                    Project = project,
                    Phase = phase,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var reportData = _reportService.GetReportData(filter);
                return Json(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report data");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetCellStaffDetail")]
        public IActionResult GetCellStaffDetail(string project, string week, string department)
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return Json(new { error = "Unauthorized" });
                }

                var staffDetails = _reportService.GetCellStaffDetail(project, week, department);
                return Json(staffDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cell staff detail");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetProjectStaffDetail")]
        public IActionResult GetProjectStaffDetail(
            string timeRange = "current_week",
            string year = "",
            string department = "",
            string project = "",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return Json(new { error = "Unauthorized" });
                }

                var filter = new ReportFilterDto
                {
                    TimeRange = timeRange,
                    Year = year,
                    Department = department,
                    Project = project,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var staffDetails = _reportService.GetProjectStaffDetail(filter, project, department);
                return Json(staffDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project staff detail");
                return Json(new { error = ex.Message });
            }
        }

        [RequireUpdate]
        [HttpGet("ExportReport")]
        public IActionResult ExportReport(
            string timeRange = "current_week",
            string year = "",
            string department = "",
            string project = "",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return RedirectToAction("DangNhap", "Account");
                }

                var filter = new ReportFilterDto
                {
                    TimeRange = timeRange,
                    Year = year,
                    Department = department,
                    Project = project,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var bytes = _reportService.ExportReportToCsv(filter);
                return File(bytes, "text/csv", $"Bao_cao_nang_suat_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                return BadRequest(new { error = ex.Message });
            }
        }

        // Thêm endpoint này vào HeatmapController.cs trước #endregion ở cuối file

        [HttpGet("GetStaffDailyDetail")]
        public IActionResult GetStaffDailyDetail(
            string timeRange = "current_week",
            string year = "",
            string department = "",
            string project = "",
            string svnStaff = "",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return Json(new { error = "Unauthorized" });
                }

                var filter = new ReportFilterDto
                {
                    TimeRange = timeRange,
                    Year = year,
                    Department = department,
                    Project = project,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var dailyDetails = _reportService.GetStaffDailyDetail(filter, project, department, svnStaff);
                return Json(dailyDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff daily detail");
                return Json(new { error = ex.Message });
            }
        }
        

        #endregion
    }
}