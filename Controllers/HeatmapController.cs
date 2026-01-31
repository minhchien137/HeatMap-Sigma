using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HeatmapSystem.Models;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    public class HeatmapController : Controller
    {
        private readonly ILogger<HeatmapController> _logger;
        private readonly ApplicationDbContext _context;
    

        public HeatmapController(ILogger<HeatmapController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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
            // Lấy danh sách bộ phận từ database
            var departments = _context.SVN_Department.ToList();
            ViewBag.Departments = departments;
            return View();
        }

        [HttpGet("Home")]
        public IActionResult Home()
        {
            return View();
        }



    }
}