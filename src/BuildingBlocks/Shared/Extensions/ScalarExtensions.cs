using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Shared.Extensions;

public static class ScalarExtensions
{
    /// <summary>
    /// Configures Scalar API Reference with shared settings across all APIs:
    /// - DeepSpace dark theme
    /// - Default C# HttpClient
    /// - Auto-inferred or customized API title
    /// </summary>
    /// <param name="endpoints">The endpoint route builder (e.g. app).</param>
    /// <param name="title">Custom title for the API documentation. If omitted, uses the application name.</param>
    /// <param name="configure">Optional callback for additional Scalar options.</param>
    /// <returns>Endpoint convention builder for further endpoint chaining.</returns>
    public static IEndpointConventionBuilder MapCustomScalarApiReference(
        this IEndpointRouteBuilder endpoints,
        string? title = null,
        Action<ScalarOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var appTitle = title ?? endpoints.ServiceProvider.GetService<IHostEnvironment>()?.ApplicationName ?? "API Reference";

        return endpoints.MapScalarApiReference(options =>
        {
            options
                .WithTitle(appTitle)
                .WithTheme(ScalarTheme.DeepSpace)          // dark theme
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            // Sidebar is shown by default — no need to call WithSidebar(true)
            // Proxy disabled: requests go directly to your API, not through proxy.scalar.com

            configure?.Invoke(options);
        });
    }
}
