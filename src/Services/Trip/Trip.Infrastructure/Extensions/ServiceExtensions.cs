using Infrastructure.Extensions;
using Matching.API.Protos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trip.Application.Common.Interfaces;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Consumers;
using Trip.Infrastructure.GrpcServices;
using Trip.Infrastructure.Persistence;
using Trip.Infrastructure.Repositories;

namespace Trip.Infrastructure.Extensions;

public static class ServiceExtensions
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

        // Register MassTransit + RabbitMQ + Transactional Outbox for SQL Server + Consumers
        services.AddCustomMassTransitWithSqlOutbox<TripDbContext>(configuration, "trip", busConfig =>
        {
            busConfig.AddConsumer<PaymentCompletedConsumer>();
            busConfig.AddConsumer<PaymentFailedConsumer>();
            busConfig.AddConsumer<TripMatchingFailedConsumer>();
        });

        // Đăng ký TripRepository
        services.AddScoped<ITripRepository, TripRepository>();

        // Register gRPC Client for Matching Service
        var matchingGrpcUrl = configuration["GrpcSettings:MatchingUrl"] ?? "http://localhost:5004";
        services.AddGrpcClient<PricingService.PricingServiceClient>(options =>
        {
            options.Address = new Uri(matchingGrpcUrl);
        });
        services.AddScoped<IPricingGrpcService, PricingGrpcService>();

        return services;
    }
}
