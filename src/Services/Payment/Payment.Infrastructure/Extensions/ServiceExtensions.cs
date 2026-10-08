using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;
using Payment.Application.Payments;
using Payment.Infrastructure.Gateway;
using Payment.Infrastructure.Messaging;
using MassTransit;

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
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.Configure<PaymentGatewayOptions>(configuration.GetSection("PaymentGateway"));
        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IPaymentEvents, PaymentEvents>();
        services.AddCustomMassTransitWithPostgresOutbox<PaymentDbContext>(configuration, bus =>
        {
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(
                configuration["EventBusSettings:EndpointPrefix"] ?? "payment", false));
            bus.AddConsumer<TripDropOffConsumer, TripDropOffConsumerDefinition>();
            bus.AddConsumer<RefundRequestedConsumer, RefundRequestedConsumerDefinition>();
        });

        return services;
    }
}
