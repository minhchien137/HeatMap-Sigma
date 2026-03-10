using HeatmapSystem.Models;
using HeatmapSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ZKBioTimeDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("ZKBioTimeConnection")));



// Thêm Session để lưu thông tin đăng nhập
builder.Services.AddSession(options =>
{
    // Tăng timeout lên 7 ngày thay vì 30 phút
    options.IdleTimeout = TimeSpan.FromDays(7);
    
    // Session cookie timeout - tương tự IdleTimeout
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    
    // THÊM: Cookie name để dễ quản lý
    options.Cookie.Name = ".HeatmapSystem.Session";
    
    // THÊM: Bảo mật - chỉ gửi cookie qua HTTPS (nếu production)
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

/*-------Report Service------ */
builder.Services.AddScoped<IReportService, ReportService>();

/*-------Log Service------ */
builder.Services.AddScoped<ILogService, LogService>();

/*-------Auth Service ------ */
builder.Services.AddScoped<IAuthService, AuthService>();

/*-------Token Cleanup Service ------ */
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();


app.Use(async (context, next) =>
{
    // Allow all HTTP methods
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        context.Response.StatusCode = 200;
        return;
    }
    await next();
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();


app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";

    // Bỏ qua: trang account, static files, api calls
    bool isAccountPage  = path.StartsWith("/account/") || path == "/account";
    bool isStaticFile   = System.IO.Path.HasExtension(path);

    if (!isAccountPage && !isStaticFile)
    {
        var svnCode = context.Session.GetString("SVNCode");

        // Session hết nhưng còn RefreshToken cookie → tự restore
        if (string.IsNullOrEmpty(svnCode) && context.Request.Cookies.ContainsKey("RefreshToken"))
        {
            try
            {
                var authService = context.RequestServices.GetRequiredService<IAuthService>();
                var db          = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                var refreshToken = context.Request.Cookies["RefreshToken"];
                var ip           = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                // Kiểm tra X-Forwarded-For nếu có proxy
                if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                    ip = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

                var ua      = context.Request.Headers["User-Agent"].ToString();
                var isValid = await authService.ValidateRefreshToken(refreshToken, ip, ua);

                if (isValid)
                {
                    var authToken = await db.AuthTokens
                        .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken
                                               && !t.IsRevoked
                                               && !t.IsUsed
                                               && t.ExpiresAt > DateTime.Now);

                    if (authToken != null)
                    {
                        var user = await db.SVN_User
                            .FirstOrDefaultAsync(u => u.SVNCode == authToken.SVNCode);

                        if (user != null)
                        {
                            // ✅ Restore session - user không bị văng ra ngoài
                            context.Session.SetString("SVNCode",    user.SVNCode);
                            context.Session.SetString("IsAdmin",    user.IsAdmin.ToString().ToLower());
                            context.Session.SetString("Permission", user.Permission ?? "None");
                        }
                    }
                }
                else
                {
                    // Token không hợp lệ → xóa cookie tránh loop
                    context.Response.Cookies.Delete("RefreshToken");
                }
            }
            catch
            {
                // Không throw exception, cứ để next() chạy, user sẽ bị redirect login bình thường
                context.Response.Cookies.Delete("RefreshToken");
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapStaticAssets();

// THÊM: Xử lý root URL redirect về trang đăng nhập
app.MapGet("/", context =>
{
    context.Response.Redirect("/Account/DangNhap");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=DangNhap}/{id?}");


app.Run();