using Infrastructure.Extensions;
using Matching.Application.Configurations;
using Matching.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

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

        return services;
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
