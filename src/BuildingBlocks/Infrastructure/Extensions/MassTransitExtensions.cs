using Infrastructure.Configurations;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class MassTransitExtensions
{
    /// <summary>
    /// Configures MassTransit with RabbitMQ without EF Core Outbox (e.g. for Matching / Mongo services).
    /// </summary>
    public static IServiceCollection AddCustomMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(settings.HostAddress, settings.Port, "/", host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Configures MassTransit with RabbitMQ and Transactional Outbox for SQL Server (Trip Service).
    /// </summary>
    public static IServiceCollection AddCustomMassTransitWithSqlOutbox<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            busConfig.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
                outbox.DisableInboxCleanupService(); // Keeps inbox records for auditing/idempotency
            });

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(settings.HostAddress, settings.Port, "/", host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Configures MassTransit with RabbitMQ and Transactional Outbox for PostgreSQL (Payment Service).
    /// </summary>
    public static IServiceCollection AddCustomMassTransitWithPostgresOutbox<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            busConfig.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.DisableInboxCleanupService();
            });

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(settings.HostAddress, settings.Port, "/", host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Configures MassTransit with RabbitMQ and Transactional Outbox for MongoDB (Matching Service).
    /// </summary>
    public static IServiceCollection AddCustomMassTransitWithMongoOutbox(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            busConfig.AddMongoDbOutbox(outbox =>
            {
                outbox.ClientFactory(provider => provider.GetRequiredService<MongoDB.Driver.IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<MongoDB.Driver.IMongoDatabase>());
                outbox.UseBusOutbox();
            });

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(settings.HostAddress, settings.Port, "/", host =>
                {
                    host.Username(settings.UserName);
                    host.Password(settings.Password);
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static EventBusSettings GetEventBusSettings(IConfiguration configuration)
    {
        var settings = new EventBusSettings();
        configuration.GetSection(EventBusSettings.SectionName).Bind(settings);
        return settings;
    }
}
