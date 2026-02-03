using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HeatmapSystem.Models;
using Microsoft.EntityFrameworkCore;

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
        public int status { get; set; }
    }

    [Route("[controller]")]
    public class HeatmapController : Controller
    {
        private readonly ILogger<HeatmapController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ZKBioTimeDbContext _zkContext;


        public HeatmapController(ILogger<HeatmapController> logger, ApplicationDbContext context, ZKBioTimeDbContext zkContext)
        {
            _logger = logger;
            _context = context;
            _zkContext = zkContext;
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
                                    status = Convert.ToInt32(reader[6])  // Đổi thành Convert.ToInt32
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
            public string WorkDate { get; set; }
            public decimal WorkHours { get; set; }
        }

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
                                    status = Convert.ToInt32(reader["status"])
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
                        Project = project.NameProject,
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


        // Thêm các methods này vào HeatmapController.cs

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
                        project = s.Project,
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

        [HttpDelete("DeleteStaffDetail/{id}")]
        public IActionResult DeleteStaffDetail(int id)
        {
            try
            {
                // Kiểm tra authentication
                if (!IsAuthenticated())
                {
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });
                }

                var record = _context.SVN_StaffDetail.Find(id);
                if (record == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bản ghi" });
                }

                _context.SVN_StaffDetail.Remove(record);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff detail {Id}", id);
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

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

        [HttpGet("ExportHistoryToExcel")]
        public IActionResult ExportHistoryToExcel(
            string department = "",
            string project = "",
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
                csv.AppendLine("STT,SVN Staff,Tên nhân viên,Bộ phận,Dự án,Ngày làm việc,Tuần,Năm,Giờ làm,Người tạo,Ngày tạo");

                int stt = 1;
                foreach (var item in data)
                {
                    csv.AppendLine($"{stt},{item.SVNStaff},{item.NameStaff},{item.Department},{item.Project}," +
                        $"{item.WorkDate:dd/MM/yyyy},{item.WeekNo},{item.Year},{item.WorkHours}," +
                        $"{item.CreateBy},{item.CreateDate:dd/MM/yyyy HH:mm}");
                    stt++;
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"Lich_su_nhap_lieu_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting history to Excel");
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}

