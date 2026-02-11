using HeatmapSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeatmapSystem.Services
{
 
    /* 
       1. Background service chạy định kỳ để dọn dẹp token hết hạn
       2. Chạy mỗi ngày lúc 2:00 AM
    */

    

    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Chạy mỗi 24 giờ

        public TokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service đã khởi động");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tính thời gian đến 2:00 AM tiếp theo
                    var now = DateTime.Now;
                    var nextRun = now.Date.AddDays(1).AddHours(2); // 2:00 AM ngày mai
                    
                    if (now.Hour < 2)
                    {
                        nextRun = now.Date.AddHours(2); // 2:00 AM hôm nay
                    }

                    var delay = nextRun - now;
                    
                    _logger.LogInformation($"Token cleanup sẽ chạy lúc {nextRun:yyyy-MM-dd HH:mm:ss}");
                    
                    await Task.Delay(delay, stoppingToken);

                    // Chạy cleanup
                    await CleanupTokens();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong Token Cleanup Service");
                    // Nếu có lỗi, đợi 1 giờ rồi thử lại
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Token Cleanup Service đã dừng");
        }

        private async Task CleanupTokens()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    
                    _logger.LogInformation("Bắt đầu dọn dẹp token hết hạn...");
                    await authService.CleanupExpiredTokens();
                    _logger.LogInformation("Hoàn thành dọn dẹp token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dọn dẹp token");
            }
        }
    }
}
