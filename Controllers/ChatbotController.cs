using HeatmapSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatmapSystem.Controllers
{
    [Route("[controller]")]
    public class ChatbotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(ApplicationDbContext context, ILogger<ChatbotController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /Chatbot/GetFAQ?lang=vi  — trả về FAQ active theo ngôn ngữ
        [HttpGet("GetFAQ")]
        public async Task<IActionResult> GetFAQ(string lang = "vi")
        {
            try
            {
                var language = (lang == "en") ? "en" : (lang == "cn") ? "cn" : "vi";

                var faqs = await _context.SVN_ChatbotFAQ
                    .Where(f => f.IsActive && f.Language == language)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new
                    {
                        f.Id,
                        f.Question,
                        f.Answer,
                        f.Keywords,
                        f.Category
                    })
                    .ToListAsync();

                return Json(faqs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy FAQ chatbot");
                return Json(new List<object>());
            }
        }

        // POST /Chatbot/Search — tìm kiếm theo từ khóa và ngôn ngữ
        [HttpPost("Search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Query))
                    return Json(new List<object>());

                var query    = request.Query.Trim().ToLower();
                var language = (request.Lang == "en") ? "en" : (request.Lang == "cn") ? "cn" : "vi";

                var faqs = await _context.SVN_ChatbotFAQ
                    .Where(f => f.IsActive && f.Language == language)
                    .OrderBy(f => f.SortOrder)
                    .ToListAsync();

                var results = faqs
                    .Where(f =>
                        f.Question.ToLower().Contains(query) ||
                        (f.Keywords != null && f.Keywords.ToLower().Split(',')
                            .Any(k => k.Trim().Contains(query) || query.Contains(k.Trim()))))
                    .Take(5)
                    .Select(f => new { f.Id, f.Question, f.Answer, f.Category })
                    .ToList();

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm FAQ");
                return Json(new List<object>());
            }
        }

        public class SearchRequest
        {
            public string Query { get; set; }
            public string Lang  { get; set; } = "cn";
        }
    }
}