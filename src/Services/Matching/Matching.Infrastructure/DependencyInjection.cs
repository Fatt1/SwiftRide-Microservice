using Infrastructure.Extensions;
using Matching.Application.Configurations;
using Matching.Domain.Repositories;
using Matching.Infrastructure.Configurations;
using Matching.Infrastructure.Persistence;
using Matching.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.CQRS.Behaviors;

namespace Matching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMatchingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configure MongoDB Client & Database
        services.ConfigureMongoDbClient(configuration);

        // 2. Register MassTransit + RabbitMQ + MongoDB Transactional Outbox
        services.AddCustomMassTransitWithMongoOutbox(configuration);

        // 3. Register Pricing Configuration (Options Pattern)
        services.Configure<PricingConfig>(configuration.GetSection(PricingConfig.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<PricingConfig>>().Value);

        // 4. Register Repositories
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
            cfg.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
    }

    private static void ConfigureMongoDbClient(this IServiceCollection services, IConfiguration configuration)
    {
        MongoDbConfigurator.ConfigureConventions();

        // 1. Đăng ký MongoDbSettings theo Options Pattern
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));

        // Đăng ký instance MongoDbSettings dạng Singleton từ IOptions để có thể inject trực tiếp nếu cần
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

        // 2. Đăng ký IMongoClient từ MongoDbSettings
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<MongoDbSettings>();
            return new MongoClient(settings.ConnectionString);
        });

        // 3. Đăng ký IMongoDatabase từ MongoDbSettings
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var settings = sp.GetRequiredService<MongoDbSettings>();
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(settings.DatabaseName);
        });

        // 4. Register MongoDb Initializer (Indexes & Seeding)
        services.AddHostedService<MongoDbInitializer>();
    }
}
