using Microsoft.Extensions.Logging;

namespace Payment.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PaymentDbContext context, ILogger logger)
    {
        // Custom seeding logic here if needed
        await Task.CompletedTask;
    }
}
