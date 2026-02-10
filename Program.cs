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
    // THAY ĐỔI: Tăng timeout lên 7 ngày thay vì 30 phút
    options.IdleTimeout = TimeSpan.FromDays(7);
    
    // THÊM: Session cookie timeout - tương tự IdleTimeout
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


var app = builder.Build();

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
