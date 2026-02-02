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
            return View();
        }

        [HttpGet("Setting")]
        public IActionResult Setting()
        {
            return View();
        }
    }
}