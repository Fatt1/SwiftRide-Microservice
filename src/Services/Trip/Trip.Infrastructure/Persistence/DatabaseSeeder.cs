using Microsoft.Extensions.Logging;
namespace Trip.Infrastructure.Persistence;
public static class DatabaseSeeder
{
    public static Task SeedAsync(TripDbContext context, ILogger logger) => Task.CompletedTask;
}
