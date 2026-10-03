using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Extensions;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configures OpenTelemetry Tracing and Metrics for ASP.NET Core services with OTLP Exporter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">Name of the service (reported to OpenTelemetry Collector / Jaeger / Aspire).</param>
    /// <param name="otlpEndpoint">Endpoint URL of the OTLP Collector (e.g. http://localhost:4317).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomOpenTelemetry(
        this IServiceCollection services,
        string serviceName,
        string otlpEndpoint)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }
}
