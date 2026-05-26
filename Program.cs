using HeatmapSystem.Models;
using HeatmapSystem.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ZKBioTimeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ZKBioTimeConnection")));

// ForwardedHeaders — bắt buộc khi đứng sau Nginx
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Thêm Session để lưu thông tin đăng nhập
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".HeatmapSystem.Session";
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

// PathBase phải là middleware ĐẦU TIÊN
app.UsePathBase("/heatmap");

// ForwardedHeaders ngay sau PathBase
app.UseForwardedHeaders();

// CORS preflight
app.Use(async (context, next) =>
{
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
    app.UseHsts();
}

// KHÔNG dùng UseHttpsRedirection — Nginx đã handle HTTPS rồi
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Auth middleware — restore session từ RefreshToken
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";

    bool isAccountPage = path.StartsWith("/account/") || path == "/account";
    bool isStaticFile  = System.IO.Path.HasExtension(path);

    if (!isAccountPage && !isStaticFile)
    {
        var svnCode = context.Session.GetString("SVNCode");

        if (string.IsNullOrEmpty(svnCode) && context.Request.Cookies.ContainsKey("RefreshToken"))
        {
            try
            {
                var authService = context.RequestServices.GetRequiredService<IAuthService>();
                var db          = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                var refreshToken = context.Request.Cookies["RefreshToken"];
                var ip           = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

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
                            context.Session.SetString("SVNCode",    user.SVNCode);
                            context.Session.SetString("IsAdmin",    user.IsAdmin.ToString().ToLower());
                            context.Session.SetString("Permission", user.Permission ?? "None");
                        }
                    }
                }
                else
                {
                    context.Response.Cookies.Delete("RefreshToken");
                }
            }
            catch
            {
                context.Response.Cookies.Delete("RefreshToken");
            }
        }
    }

    await next();
});

app.UseAuthorization();

// Root redirect — dùng PathBase để đúng sub-path
app.MapGet("/", context =>
{
    context.Response.Redirect(context.Request.PathBase + "/Account/DangNhap");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=DangNhap}/{id?}");

app.Run();
