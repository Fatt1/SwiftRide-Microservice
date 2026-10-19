using Infrastructure.Configurations;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class MassTransitExtensions
{
    /// <summary>
    /// Configures MassTransit with RabbitMQ without EF Core Outbox (e.g. for Identity / services without Outbox).
    /// </summary>
    public static IServiceCollection AddCustomMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        string servicePrefix = "",
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            if (!string.IsNullOrWhiteSpace(servicePrefix))
            {
                busConfig.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(servicePrefix, false));
            }

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                ConfigureRabbitMqBus(rabbit, context, settings);
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
        string servicePrefix = "",
        Action<IBusRegistrationConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            if (!string.IsNullOrWhiteSpace(servicePrefix))
            {
                busConfig.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(servicePrefix, false));
            }

            busConfig.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
                outbox.DisableInboxCleanupService(); // Keeps inbox records for auditing/idempotency
            });

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                ConfigureRabbitMqBus(rabbit, context, settings);
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
        string servicePrefix = "",
        Action<IBusRegistrationConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            if (!string.IsNullOrWhiteSpace(servicePrefix))
            {
                busConfig.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(servicePrefix, false));
            }

            busConfig.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.DisableInboxCleanupService();
            });

            busConfig.AddConfigureEndpointsCallback((context, _, endpoint) =>
                endpoint.UseEntityFrameworkOutbox<TDbContext>(context));

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                ConfigureRabbitMqBus(rabbit, context, settings);
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
        string servicePrefix = "",
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var settings = GetEventBusSettings(configuration);

        services.AddMassTransit(busConfig =>
        {
            if (!string.IsNullOrWhiteSpace(servicePrefix))
            {
                busConfig.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(servicePrefix, false));
            }

            busConfig.AddMongoDbOutbox(outbox =>
            {
                outbox.ClientFactory(provider => provider.GetRequiredService<MongoDB.Driver.IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<MongoDB.Driver.IMongoDatabase>());
                outbox.UseBusOutbox();
            });

            configure?.Invoke(busConfig);

            busConfig.UsingRabbitMq((context, rabbit) =>
            {
                ConfigureRabbitMqBus(rabbit, context, settings);
            });
        });

        return services;
    }

    private static void ConfigureRabbitMqBus(
        IRabbitMqBusFactoryConfigurator rabbit,
        IBusRegistrationContext context,
        EventBusSettings settings)
    {
        rabbit.Host(settings.HostAddress, settings.Port, "/", host =>
        {
            host.Username(settings.UserName);
            host.Password(settings.Password);
        });

        // Global Retry Policy: tự động retry khi gặp lỗi tạm thời (DB lag, network, deadlock...)
        // Bỏ qua ArgumentException vì đây là lỗi dữ liệu không hợp lệ, retry cũng sẽ thất bại.
        rabbit.UseMessageRetry(retry =>
        {
            retry.Ignore<ArgumentException>();
            retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        });

        rabbit.ConfigureEndpoints(context);
    }

    private static EventBusSettings GetEventBusSettings(IConfiguration configuration)
    {
        var settings = new EventBusSettings();
        configuration.GetSection(EventBusSettings.SectionName).Bind(settings);
        return settings;
    }
}
