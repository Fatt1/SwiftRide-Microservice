using Infrastructure.Extensions;
using Matching.Application.Configurations;
using Matching.Domain.Repositories;
using Matching.Infrastructure.Persistence;
using Matching.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shared.CQRS.Behaviors;

namespace Matching.Infrastructure;

public static class DependencyInjection
{

    public static IServiceCollection AddMatchingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PricingConfig>(configuration.GetSection(PricingConfig.SectionName));

        // 1. Configure MongoDB Client & Database
        services.ConfigureMongoDbClient(configuration);
        // 2. Register MassTransit + RabbitMQ + MongoDB Transactional Outbox
        services.AddCustomMassTransitWithMongoOutbox(configuration);


        // 4. Register Pricing Configuration (Options Pattern)
        services.Configure<PricingConfig>(configuration.GetSection(PricingConfig.SectionName));


        services.AddScoped<IMatchingRepository, MatchingRepository>();
        services.AddScoped<IDriverLocationRepository, DriverLocationRepository>();

        services.ConfigureMeditR();
        return services;
    }


    public static void ConfigureMeditR(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(DependencyInjection).Assembly,
                typeof(PricingConfig).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
    }

    private static void ConfigureMongoDbClient(this IServiceCollection services, IConfiguration configuration)
    {
        MongoDbConfigurator.ConfigureConventions();

        // 1. Configure MongoDB Client & Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["MongoDbSettings:ConnectionString"]
            ?? "mongodb://localhost:27017";

        var databaseName = configuration["MongoDbSettings:DatabaseName"] ?? "MatchingDb";

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });


        // 3. Register MongoDb Initializer (Indexes)
        services.AddHostedService<Persistence.MongoDbInitializer>();



    }
}
