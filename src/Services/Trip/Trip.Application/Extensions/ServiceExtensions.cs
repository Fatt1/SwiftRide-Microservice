using Microsoft.Extensions.DependencyInjection;

namespace Trip.Application.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddTripApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceExtensions).Assembly);
        });

        return services;
    }
}
