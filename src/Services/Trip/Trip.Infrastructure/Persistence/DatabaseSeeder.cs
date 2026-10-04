using Microsoft.Extensions.Logging;

namespace Trip.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(TripDbContext context, ILogger logger)
    {
        // Custom seeding logic here if needed
        await Task.CompletedTask;
    }
}
