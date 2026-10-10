using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Payment.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<PaymentDbContext>();
        var logger = services.GetRequiredService<ILogger<PaymentDbContext>>();

        try
        {
            logger.LogInformation("Đang kiểm tra và áp dụng migration cơ sở dữ liệu còn chờ...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Đã áp dụng migration cơ sở dữ liệu thành công.");

            var seederLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseSeeder));
            await DatabaseSeeder.SeedAsync(context, seederLogger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Có lỗi khi chạy migration hoặc khởi tạo dữ liệu cơ sở dữ liệu.");
            throw new InvalidOperationException("Database migration or seeding failed.", ex);
        }
    }

    public static Task MigrateDatabaseAsync(this IHost host) => host.Services.MigrateDatabaseAsync();
}
