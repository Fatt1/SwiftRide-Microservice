using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
            logger.LogInformation("Checking and applying pending database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");

            var seederLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseSeeder));
            var environment = services.GetRequiredService<IHostEnvironment>();
            var configuration = services.GetRequiredService<IConfiguration>();
            if (environment.IsDevelopment() && configuration.GetValue<bool>("PaymentDemo:Seed"))
                await DatabaseSeeder.SeedAsync(context, seederLogger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw new InvalidOperationException("Database migration or seeding failed.", ex);
        }
    }

    public static Task MigrateDatabaseAsync(this IHost host) => host.Services.MigrateDatabaseAsync();
}
