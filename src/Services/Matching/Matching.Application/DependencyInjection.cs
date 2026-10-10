using Microsoft.Extensions.DependencyInjection;
using Shared.CQRS.Behaviors;

namespace Matching.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMatchingApplication(this IServiceCollection services)
    {
        services.ConfigureMediatR();
        return services;
    }

    public static void ConfigureMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
    }
}
