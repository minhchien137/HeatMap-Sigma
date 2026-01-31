using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    public class ViidooController : Controller
    {
        private readonly ILogger<ViidooController> _logger;

        public ViidooController(ILogger<ViidooController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetEmployeeByEmpCode")]
        public async Task<IActionResult> GetEmployeeByEmpCode(string empCode)
        {
            if (string.IsNullOrWhiteSpace(empCode))
                return BadRequest("empCode is required");

            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var client = new HttpClient(handler);

                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Authorization",
                    "Bearer PUT_JWT_TOKEN_HERE");

                var url =
                    $"https://10.10.99.10:8443/personnel/api/employees/?emp_code={empCode}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, content);

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEmployeeByEmpCode error");
                return StatusCode(500, ex.Message);
            }
        }



        [HttpGet("GetEmployeeById/{employeeId}")]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            try
            {
                var cookieContainer = new System.Net.CookieContainer();

                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = cookieContainer,
                    AllowAutoRedirect = true,
                    ServerCertificateCustomValidationCallback =
                        (msg, cert, chain, errors) => true
                };

                using var client = new HttpClient(handler);

                // ⚠️ GIẢ ĐỊNH: user đã login trước (cookie có sẵn)
                // Nếu cần login tự động → mình sẽ tách AuthService cho bạn

                var apiUrl =
                    $"https://10.10.99.10:8443/personnel/api/employees/?select_employee={employeeId}";

                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                // Header giống browser
                request.Headers.Add("Accept", "application/json, text/javascript, */*");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Referrer = new Uri("https://10.10.99.10:8443/");
                request.Headers.Add("Origin", "https://10.10.99.10:8443");

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, json);

                if (!json.TrimStart().StartsWith("{"))
                    return StatusCode(500, "API không trả JSON");

                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEmployeeById error");
                return StatusCode(500, ex.Message);
            }
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
