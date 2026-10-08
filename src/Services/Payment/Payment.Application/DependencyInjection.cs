using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Payments;
using Shared.CQRS.Behaviors;

namespace Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        services.AddOptions<PaymentGatewayOptions>().Validate(o => o.TimeoutSeconds > 0 && o.RetryBaseMilliseconds >= 0);
        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreatePaymentValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IPaymentProcessor, WalletPaymentProcessor>();
        services.AddScoped<IPaymentProcessor, CardPaymentProcessor>();
        services.AddScoped<PaymentProcessorFactory>();
        return services;
    }
}
