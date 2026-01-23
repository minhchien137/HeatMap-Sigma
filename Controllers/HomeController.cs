using System.Diagnostics;
using HeatmapSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace HeatmapSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Kiểm tra đăng nhập
            var svnCode = HttpContext.Session.GetString("SVNCode");
            if (string.IsNullOrEmpty(svnCode))
            {
                return RedirectToAction("DangNhap", "Account");
            }
            
            ViewBag.SVNCode = svnCode;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}