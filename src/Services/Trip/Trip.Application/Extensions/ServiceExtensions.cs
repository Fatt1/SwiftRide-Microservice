using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Shared.CQRS.Behaviors;

namespace Trip.Application.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddTripApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceExtensions).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
