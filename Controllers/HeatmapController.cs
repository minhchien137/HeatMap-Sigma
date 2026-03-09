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
    public class DepartmentNameDto
    {
        public string deptName { get; set; }
    }

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

            // Tìm bộ phận của user đang đăng nhập (nếu không phải admin)
            var isAdminImport = HttpContext.Session.GetString("IsAdmin")?.ToLower() == "true";
            var svnCodeImport = GetCurrentSVNCode();
            int? userDeptId = null;
            if (!isAdminImport && !string.IsNullOrEmpty(svnCodeImport))
            {
                using var conn = _zkContext.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TOP 1 department_id FROM personnel_employee WHERE emp_code = @code";
                var param = cmd.CreateParameter();
                param.ParameterName = "@code";
                param.Value = svnCodeImport;
                cmd.Parameters.Add(param);
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    userDeptId = Convert.ToInt32(result);
            }
            ViewBag.UserDepartmentId = userDeptId;

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
                var isAdmin = HttpContext.Session.GetString("IsAdmin")?.ToLower() == "true";
                var svnCode = GetCurrentSVNCode();

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

                // Neu khong phai admin -> chi tra ve nhan vien cung bo phan voi user dang nhap
                string userDepartment = null;
                if (!isAdmin && !string.IsNullOrEmpty(svnCode))
                {
                    var currentUser = staffData.FirstOrDefault(e =>
                        e.emp_code.Equals(svnCode, StringComparison.OrdinalIgnoreCase));
                    if (currentUser != null && !string.IsNullOrEmpty(currentUser.department))
                    {
                        userDepartment = currentUser.department;
                        staffData = staffData
                            .Where(e => e.department == userDepartment)
                            .ToList();
                    }
                }

                return Json(new { data = staffData, userDepartment, isAdmin });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff data");
                return Json(new { data = new List<StaffDto>(), userDepartment = (string)null, isAdmin = false });
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

                var isAdmin = HttpContext.Session.GetString("IsAdmin")?.ToLower() == "true";
                var svnCode = GetCurrentSVNCode();

                // Lấy bộ phận của user từ ZKBio bằng 1 query JOIN duy nhất
                string userDepartment = null;
                if (!isAdmin && !string.IsNullOrEmpty(svnCode))
                {
                    var deptResult = _zkContext.Database
                        .SqlQueryRaw<DepartmentNameDto>(@"
                            SELECT TOP 1 ISNULL(d.dept_name, '') AS deptName
                            FROM personnel_employee e
                            LEFT JOIN personnel_department d ON e.department_id = d.id
                            WHERE e.emp_code = {0}", svnCode)
                        .ToList()
                        .FirstOrDefault();

                    if (deptResult != null && !string.IsNullOrEmpty(deptResult.deptName))
                        userDepartment = deptResult.deptName;

                    // Fallback: lấy từ SVN_StaffDetail nếu ZKBio không có
                    if (string.IsNullOrEmpty(userDepartment))
                    {
                        userDepartment = _context.SVN_StaffDetail
                            .Where(s => s.SVNStaff == svnCode && s.Department != null && s.Department != "")
                            .Select(s => s.Department)
                            .FirstOrDefault();
                    }
                }

                // Lấy dữ liệu từ bảng SVN_StaffDetail
                var query = _context.SVN_StaffDetail.AsQueryable();

                // Nếu không phải admin và tìm được bộ phận → chỉ lấy bản ghi của bộ phận đó
                if (!isAdmin && !string.IsNullOrEmpty(userDepartment))
                {
                    query = query.Where(s => s.Department == userDepartment);
                }

                var data = query
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
                        workDate = s.WorkDate,
                        weekNo = s.WeekNo,
                        year = s.Year,
                        workHours = s.WorkHours,
                        createBy = s.CreateBy,
                        createDate = s.CreateDate
                    })
                    .ToList();

                return Json(new { data, userDepartment, isAdmin });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading history data");
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost("DeleteStaffDetail/{id}")]
        public async Task<IActionResult> DeleteStaffDetail(int id)
        {
            try
            {
                if (!IsAuthenticated())
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Chỉ Admin mới được xóa
                var isAdmin = HttpContext.Session.GetString("IsAdmin");
                if (isAdmin != "true")
                {
                    Response.StatusCode = 403;
                    return Json(new { success = false, message = "Bạn không có quyền xóa bản ghi!" });
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
                csv.AppendLine("STT,SVN Staff,Tên nhân viên,Bộ phận,Customer,Dự án,Project Phase,Ngày làm việc,Tuần,Năm,Giờ làm,Người tạo,Ngày tạo");

                int stt = 1;
                foreach (var item in data)
                {
                    csv.AppendLine($"{stt},{item.SVNStaff},{item.NameStaff},{item.Department},{item.Customer},{item.Project}," +
                        $"{item.ProjectPhase}," +
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

        [HttpGet("GetCustomerList")]
        public IActionResult GetCustomerList()
        {
            try
            {
                var customers = _reportService.GetCustomerList();
                return Json(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer list");
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

        [HttpGet("GetReportData")]
        public IActionResult GetReportData(
            string timeRange = "current_week",
            string year = "",
            string customer = "",
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
                    Customer = customer,
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
            string customer = "",
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
                    return RedirectToAction("DangNhap", "Account");
                }

                var filter = new ReportFilterDto
                {
                    TimeRange  = timeRange,
                    Year       = year,
                    Customer   = customer,
                    Department = department,
                    Project    = project,
                    Phase      = phase,
                    StartDate  = startDate,
                    EndDate    = endDate
                };

                var bytes = _reportService.ExportReportToCsv(filter);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Bao_cao_nang_suat_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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
        

        // ============================================================
        // MODE 1 MỚI: 1 người - 1 ngày - NHIỀU dự án
        // ============================================================

        public class ProjectRowRequest
        {
            public int ProjectId { get; set; }
            public string Customer { get; set; }
            public string ProjectPhase { get; set; }
            public decimal WorkHours { get; set; }
        }

        public class SaveStaffDetailMultiRequest
        {
            public int EmployeeId { get; set; }
            public string WorkDate { get; set; }
            public List<ProjectRowRequest> Projects { get; set; }
        }

        [RequireUpdate]
        [HttpPost("SaveStaffDetailMulti")]
        public IActionResult SaveStaffDetailMulti([FromBody] SaveStaffDetailMultiRequest request)
        {
            try
            {
                if (!IsAuthenticated())
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });

                if (request.Projects == null || request.Projects.Count == 0)
                    return Json(new { success = false, message = "Vui lòng nhập ít nhất 1 dự án" });

                // 1. Lấy thông tin nhân viên & bộ phận
                EmployeeDto employee = null;
                DepartmentDto department = null;

                using (var connection = _zkContext.Database.GetDbConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT id, emp_code, first_name, last_name, nickname, department_id, status
                                            FROM personnel_employee WHERE id = @empId";
                        var p = cmd.CreateParameter(); p.ParameterName = "@empId"; p.Value = request.EmployeeId;
                        cmd.Parameters.Add(p);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                employee = new EmployeeDto
                                {
                                    id             = Convert.ToInt32(r["id"]),
                                    emp_code       = r.IsDBNull(r.GetOrdinal("emp_code"))   ? "" : r.GetString(r.GetOrdinal("emp_code")),
                                    first_name     = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString(r.GetOrdinal("first_name")),
                                    last_name      = r.IsDBNull(r.GetOrdinal("last_name"))  ? "" : r.GetString(r.GetOrdinal("last_name")),
                                    nickname       = r.IsDBNull(r.GetOrdinal("nickname"))   ? "" : r.GetString(r.GetOrdinal("nickname")),
                                    department_id  = Convert.ToInt32(r["department_id"]),
                                    status         = Convert.ToInt16(r["status"])
                                };
                        }
                    }
                    if (employee == null)
                        return Json(new { success = false, message = "Không tìm thấy nhân viên" });

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT id, dept_code, dept_name, is_default, parent_dept_id, dept_manager_id, company_id
                                            FROM personnel_department WHERE id = @deptId";
                        var p = cmd.CreateParameter(); p.ParameterName = "@deptId"; p.Value = employee.department_id;
                        cmd.Parameters.Add(p);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                department = new DepartmentDto
                                {
                                    id             = Convert.ToInt32(r["id"]),
                                    dept_code      = r.IsDBNull(r.GetOrdinal("dept_code"))  ? "" : r.GetString(r.GetOrdinal("dept_code")),
                                    dept_name      = r.IsDBNull(r.GetOrdinal("dept_name"))  ? "" : r.GetString(r.GetOrdinal("dept_name")),
                                    is_default     = Convert.ToBoolean(r["is_default"]),
                                    parent_dept_id = r.IsDBNull(r.GetOrdinal("parent_dept_id")) ? (int?)null : Convert.ToInt32(r["parent_dept_id"]),
                                    dept_manager_id= r.IsDBNull(r.GetOrdinal("dept_manager_id"))? (int?)null : Convert.ToInt32(r["dept_manager_id"]),
                                    company_id     = Convert.ToInt32(r["company_id"])
                                };
                        }
                    }
                }

                // 2. Parse ngày
                if (!DateTime.TryParse(request.WorkDate, out DateTime workDate))
                    return Json(new { success = false, message = "Ngày làm việc không hợp lệ" });

                string currentUser = GetCurrentSVNCode();
                string fullName    = $"{employee.first_name} {employee.last_name}".Trim();
                if (string.IsNullOrEmpty(fullName)) fullName = employee.nickname ?? employee.emp_code;

                var culture = System.Globalization.CultureInfo.CurrentCulture;
                int weekNo  = culture.Calendar.GetWeekOfYear(workDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                int year    = workDate.Year;

                int savedCount = 0, updatedCount = 0;

                // 3. Loop từng dự án → lưu 1 bản ghi
                foreach (var row in request.Projects)
                {
                    var project = _context.SVN_Projects.Find(row.ProjectId);
                    if (project == null) continue;

                    // Check trùng theo (emp_code + ngày + project + projectPhase)
                    var existing = _context.SVN_StaffDetail.FirstOrDefault(s =>
                        s.SVNStaff        == employee.emp_code &&
                        s.WorkDate.Date   == workDate.Date     &&
                        s.Project         == project.NameProject &&
                        s.ProjectPhase    == (row.ProjectPhase ?? ""));

                    if (existing != null)
                    {
                        existing.WorkHours   = row.WorkHours;
                        existing.Customer    = row.Customer ?? project.NameCustomer ?? "";
                        existing.CreateDate  = DateTime.Now;
                        existing.CreateBy    = currentUser;
                        updatedCount++;
                    }
                    else
                    {
                        _context.SVN_StaffDetail.Add(new SVN_StaffDetail
                        {
                            SVNStaff     = employee.emp_code,
                            NameStaff    = fullName,
                            Department   = department?.dept_name ?? "N/A",
                            Customer     = row.Customer ?? project.NameCustomer ?? "",
                            Project      = project.NameProject,
                            ProjectPhase = row.ProjectPhase ?? "",
                            WorkDate     = workDate,
                            WeekNo       = weekNo,
                            Year         = year,
                            WorkHours    = row.WorkHours,
                            CreateBy     = currentUser,
                            CreateDate   = DateTime.Now
                        });
                        savedCount++;
                    }
                }

                _context.SaveChanges();

                return Json(new
                {
                    success      = true,
                    message      = "Lưu dữ liệu thành công",
                    savedCount   = savedCount,
                    updatedCount = updatedCount,
                    total        = savedCount + updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveStaffDetailMulti");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ============================================================
        // MODE 2 MỚI: 1 người - NHIỀU ngày - NHIỀU dự án/ngày
        // ============================================================

        public class MultiProjectDayRequest
        {
            public string Date { get; set; }
            public List<ProjectRowRequest> Projects { get; set; }
        }

        public class SaveMultipleDaysMultiRequest
        {
            public int EmployeeId { get; set; }
            public List<MultiProjectDayRequest> Days { get; set; }
        }

        [RequireUpdate]
        [HttpPost("SaveMultipleDaysMulti")]
        public IActionResult SaveMultipleDaysMulti([FromBody] SaveMultipleDaysMultiRequest request)
        {
            try
            {
                if (!IsAuthenticated())
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });

                if (request.Days == null || request.Days.Count == 0)
                    return Json(new { success = false, message = "Không có ngày nào được gửi lên" });

                // 1. Lấy nhân viên & bộ phận
                EmployeeDto employee = null;
                DepartmentDto department = null;

                using (var connection = _zkContext.Database.GetDbConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT id, emp_code, first_name, last_name, nickname, department_id, status
                                            FROM personnel_employee WHERE id = @empId";
                        var p = cmd.CreateParameter(); p.ParameterName = "@empId"; p.Value = request.EmployeeId;
                        cmd.Parameters.Add(p);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                employee = new EmployeeDto
                                {
                                    id            = Convert.ToInt32(r["id"]),
                                    emp_code      = r.IsDBNull(r.GetOrdinal("emp_code"))   ? "" : r.GetString(r.GetOrdinal("emp_code")),
                                    first_name    = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString(r.GetOrdinal("first_name")),
                                    last_name     = r.IsDBNull(r.GetOrdinal("last_name"))  ? "" : r.GetString(r.GetOrdinal("last_name")),
                                    nickname      = r.IsDBNull(r.GetOrdinal("nickname"))   ? "" : r.GetString(r.GetOrdinal("nickname")),
                                    department_id = Convert.ToInt32(r["department_id"]),
                                    status        = Convert.ToInt16(r["status"])
                                };
                        }
                    }
                    if (employee == null)
                        return Json(new { success = false, message = "Không tìm thấy nhân viên" });

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT id, dept_code, dept_name, is_default, parent_dept_id, dept_manager_id, company_id
                                            FROM personnel_department WHERE id = @deptId";
                        var p = cmd.CreateParameter(); p.ParameterName = "@deptId"; p.Value = employee.department_id;
                        cmd.Parameters.Add(p);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                department = new DepartmentDto
                                {
                                    id              = Convert.ToInt32(r["id"]),
                                    dept_code       = r.IsDBNull(r.GetOrdinal("dept_code"))  ? "" : r.GetString(r.GetOrdinal("dept_code")),
                                    dept_name       = r.IsDBNull(r.GetOrdinal("dept_name"))  ? "" : r.GetString(r.GetOrdinal("dept_name")),
                                    is_default      = Convert.ToBoolean(r["is_default"]),
                                    parent_dept_id  = r.IsDBNull(r.GetOrdinal("parent_dept_id"))  ? (int?)null : Convert.ToInt32(r["parent_dept_id"]),
                                    dept_manager_id = r.IsDBNull(r.GetOrdinal("dept_manager_id")) ? (int?)null : Convert.ToInt32(r["dept_manager_id"]),
                                    company_id      = Convert.ToInt32(r["company_id"])
                                };
                        }
                    }
                }

                string currentUser = GetCurrentSVNCode();
                string fullName    = $"{employee.first_name} {employee.last_name}".Trim();
                if (string.IsNullOrEmpty(fullName)) fullName = employee.nickname ?? employee.emp_code;

                var culture      = System.Globalization.CultureInfo.CurrentCulture;
                int savedCount   = 0;
                int updatedCount = 0;

                // 2. Loop ngày → loop dự án → lưu từng bản ghi
                foreach (var dayData in request.Days)
                {
                    if (!DateTime.TryParse(dayData.Date, out DateTime workDate)) continue;
                    if (dayData.Projects == null || dayData.Projects.Count == 0) continue;

                    int weekNo = culture.Calendar.GetWeekOfYear(workDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    int year   = workDate.Year;

                    foreach (var row in dayData.Projects)
                    {
                        var project = _context.SVN_Projects.Find(row.ProjectId);
                        if (project == null) continue;

                        var existing = _context.SVN_StaffDetail.FirstOrDefault(s =>
                            s.SVNStaff      == employee.emp_code      &&
                            s.WorkDate.Date == workDate.Date           &&
                            s.Project       == project.NameProject     &&
                            s.ProjectPhase  == (row.ProjectPhase ?? ""));

                        if (existing != null)
                        {
                            existing.WorkHours  = row.WorkHours;
                            existing.Customer   = row.Customer ?? project.NameCustomer ?? "";
                            existing.CreateDate = DateTime.Now;
                            existing.CreateBy   = currentUser;
                            updatedCount++;
                        }
                        else
                        {
                            _context.SVN_StaffDetail.Add(new SVN_StaffDetail
                            {
                                SVNStaff     = employee.emp_code,
                                NameStaff    = fullName,
                                Department   = department?.dept_name ?? "N/A",
                                Customer     = row.Customer ?? project.NameCustomer ?? "",
                                Project      = project.NameProject,
                                ProjectPhase = row.ProjectPhase ?? "",
                                WorkDate     = workDate,
                                WeekNo       = weekNo,
                                Year         = year,
                                WorkHours    = row.WorkHours,
                                CreateBy     = currentUser,
                                CreateDate   = DateTime.Now
                            });
                            savedCount++;
                        }
                    }
                }

                _context.SaveChanges();

                return Json(new
                {
                    success      = true,
                    message      = "Lưu dữ liệu thành công",
                    savedCount   = savedCount,
                    updatedCount = updatedCount,
                    total        = savedCount + updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveMultipleDaysMulti");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ============================================================
        // MODE 3 MỚI: NHIỀU người - NHIỀU ngày - NHIỀU dự án
        // ============================================================

        public class BulkImportRowRequest
        {
            public int EmpId { get; set; }
            public string Date { get; set; }
            public string Customer { get; set; }
            public int ProjectId { get; set; }
            public string ProjectPhase { get; set; }
            public decimal Hours { get; set; }
        }

        [RequireUpdate]
        [HttpPost("BulkImportMultiProject")]
        public IActionResult BulkImportMultiProject([FromBody] List<BulkImportRowRequest> records)
        {
            try
            {
                if (!IsAuthenticated())
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });

                if (records == null || records.Count == 0)
                    return Json(new { success = false, message = "Không có dữ liệu để lưu" });

                string currentUser = GetCurrentSVNCode();
                var culture        = System.Globalization.CultureInfo.CurrentCulture;

                // Cache để tránh query lặp cho cùng 1 nhân viên
                var employeeCache   = new Dictionary<int, EmployeeDto>();
                var departmentCache = new Dictionary<int, DepartmentDto>();

                // Mở 1 connection duy nhất cho toàn bộ batch
                using (var connection = _zkContext.Database.GetDbConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    // Load tất cả empId cần dùng
                    var empIds = records.Select(r => r.EmpId).Distinct().ToList();

                    foreach (var empId in empIds)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = @"SELECT id, emp_code, first_name, last_name, nickname, department_id, status
                                                FROM personnel_employee WHERE id = @empId";
                            var p = cmd.CreateParameter(); p.ParameterName = "@empId"; p.Value = empId;
                            cmd.Parameters.Add(p);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    var emp = new EmployeeDto
                                    {
                                        id            = Convert.ToInt32(r["id"]),
                                        emp_code      = r.IsDBNull(r.GetOrdinal("emp_code"))   ? "" : r.GetString(r.GetOrdinal("emp_code")),
                                        first_name    = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString(r.GetOrdinal("first_name")),
                                        last_name     = r.IsDBNull(r.GetOrdinal("last_name"))  ? "" : r.GetString(r.GetOrdinal("last_name")),
                                        nickname      = r.IsDBNull(r.GetOrdinal("nickname"))   ? "" : r.GetString(r.GetOrdinal("nickname")),
                                        department_id = Convert.ToInt32(r["department_id"]),
                                        status        = Convert.ToInt16(r["status"])
                                    };
                                    employeeCache[empId] = emp;
                                }
                            }
                        }
                    }

                    // Load tất cả department cần dùng
                    var deptIds = employeeCache.Values.Select(e => e.department_id).Distinct().ToList();

                    foreach (var deptId in deptIds)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = @"SELECT id, dept_code, dept_name, is_default, parent_dept_id, dept_manager_id, company_id
                                                FROM personnel_department WHERE id = @deptId";
                            var p = cmd.CreateParameter(); p.ParameterName = "@deptId"; p.Value = deptId;
                            cmd.Parameters.Add(p);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                    departmentCache[deptId] = new DepartmentDto
                                    {
                                        id              = Convert.ToInt32(r["id"]),
                                        dept_code       = r.IsDBNull(r.GetOrdinal("dept_code"))  ? "" : r.GetString(r.GetOrdinal("dept_code")),
                                        dept_name       = r.IsDBNull(r.GetOrdinal("dept_name"))  ? "" : r.GetString(r.GetOrdinal("dept_name")),
                                        is_default      = Convert.ToBoolean(r["is_default"]),
                                        parent_dept_id  = r.IsDBNull(r.GetOrdinal("parent_dept_id"))  ? (int?)null : Convert.ToInt32(r["parent_dept_id"]),
                                        dept_manager_id = r.IsDBNull(r.GetOrdinal("dept_manager_id")) ? (int?)null : Convert.ToInt32(r["dept_manager_id"]),
                                        company_id      = Convert.ToInt32(r["company_id"])
                                    };
                            }
                        }
                    }
                }

                int savedCount = 0, updatedCount = 0, skippedCount = 0;

                foreach (var record in records)
                {
                    if (!employeeCache.TryGetValue(record.EmpId, out var employee)) { skippedCount++; continue; }
                    if (!DateTime.TryParse(record.Date, out DateTime workDate))     { skippedCount++; continue; }

                    var project = _context.SVN_Projects.Find(record.ProjectId);
                    if (project == null) { skippedCount++; continue; }

                    departmentCache.TryGetValue(employee.department_id, out var department);

                    string fullName = $"{employee.first_name} {employee.last_name}".Trim();
                    if (string.IsNullOrEmpty(fullName)) fullName = employee.nickname ?? employee.emp_code;

                    int weekNo = culture.Calendar.GetWeekOfYear(workDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    int year   = workDate.Year;

                    var existing = _context.SVN_StaffDetail.FirstOrDefault(s =>
                        s.SVNStaff      == employee.emp_code       &&
                        s.WorkDate.Date == workDate.Date            &&
                        s.Project       == project.NameProject      &&
                        s.ProjectPhase  == (record.ProjectPhase ?? ""));

                    if (existing != null)
                    {
                        existing.WorkHours  = record.Hours;
                        existing.Customer   = record.Customer ?? project.NameCustomer ?? "";
                        existing.CreateDate = DateTime.Now;
                        existing.CreateBy   = currentUser;
                        updatedCount++;
                    }
                    else
                    {
                        _context.SVN_StaffDetail.Add(new SVN_StaffDetail
                        {
                            SVNStaff     = employee.emp_code,
                            NameStaff    = fullName,
                            Department   = department?.dept_name ?? "N/A",
                            Customer     = record.Customer ?? project.NameCustomer ?? "",
                            Project      = project.NameProject,
                            ProjectPhase = record.ProjectPhase ?? "",
                            WorkDate     = workDate,
                            WeekNo       = weekNo,
                            Year         = year,
                            WorkHours    = record.Hours,
                            CreateBy     = currentUser,
                            CreateDate   = DateTime.Now
                        });
                        savedCount++;
                    }
                }

                _context.SaveChanges();

                return Json(new
                {
                    success      = true,
                    message      = $"Đã xử lý {records.Count} bản ghi",
                    savedCount   = savedCount,
                    updatedCount = updatedCount,
                    skippedCount = skippedCount,
                    total        = savedCount + updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BulkImportMultiProject");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        #endregion
    }
}