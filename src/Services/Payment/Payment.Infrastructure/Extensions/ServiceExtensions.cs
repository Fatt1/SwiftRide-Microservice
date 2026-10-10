using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;
using Payment.Application.Abstractions;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Repositories;
using Payment.Infrastructure.Strategies;
using Payment.Infrastructure.Factories;
using Payment.Infrastructure.Payments;
using Payment.Infrastructure.Messaging;
using MassTransit;
using MassTransit.Logging;
using OpenTelemetry.Trace;
using System.Data;
using MassTransit.EntityFrameworkCoreIntegration;

namespace Payment.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");


        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName);
            }));

        // Register MassTransit + RabbitMQ + Transactional Outbox for PostgreSQL
        services.AddScoped<PaymentRepository>();
        services.AddScoped<IPaymentRepository>(provider => provider.GetRequiredService<PaymentRepository>());
        services.AddScoped<IPaymentTransaction>(provider => provider.GetRequiredService<PaymentRepository>());
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IPaymentStrategy, WalletPaymentStrategy>();
        services.AddScoped<IPaymentStrategy, CardPaymentStrategy>();
        services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
        services.AddScoped<ICardPaymentProcessor, MockCardPaymentProcessor>();
        services.AddCustomMassTransitWithPostgresOutbox<PaymentDbContext>(configuration, bus =>
            bus.AddConsumer<TripDropOffConsumer, TripDropOffConsumerDefinition>());
        services.PostConfigure<EntityFrameworkOutboxOptions>(options =>
            options.IsolationLevel = IsolationLevel.ReadCommitted);
        services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(DiagnosticHeaders.DefaultListenerName));

        return services;
    }
}
