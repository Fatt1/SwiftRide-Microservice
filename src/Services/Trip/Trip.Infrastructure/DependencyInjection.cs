using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trip.Infrastructure.Persistence;

namespace Trip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTripInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1435;Database=tripdb;User Id=sa;Password=Password@123;TrustServerCertificate=True;";

        services.AddDbContext<TripDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(TripDbContext).Assembly.FullName);
            }));

        // Register MassTransit + RabbitMQ + Transactional Outbox for SQL Server
        services.AddCustomMassTransitWithSqlOutbox<TripDbContext>(configuration);

        return services;
    }
}
