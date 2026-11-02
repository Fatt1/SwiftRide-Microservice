using System.Globalization;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace Common.Logging;

public static class Serilogger
{
    public static Action<HostBuilderContext, LoggerConfiguration> Configure =>
        (context, configuration) =>
        {
            var applicationName = context.HostingEnvironment.ApplicationName?.ToUpperInvariant().Replace(".", "-", StringComparison.Ordinal);
            var environmentName = context.HostingEnvironment.EnvironmentName ?? "Development";

            configuration
                .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    formatProvider: CultureInfo.InvariantCulture)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Environment", environmentName)
                .Enrich.WithProperty("Application", applicationName ?? string.Empty)
                .ReadFrom.Configuration(context.Configuration);

            var lokiUrl = context.Configuration["Loki:Url"] ?? context.Configuration["LokiSettings:Url"];
            if (!string.IsNullOrWhiteSpace(lokiUrl))
            {
                configuration.WriteTo.GrafanaLoki(
                    lokiUrl,
                    labels: new[]
                    {
                        new LokiLabel { Key = "app", Value = applicationName?.ToLowerInvariant() ?? "unknown" },
                        new LokiLabel { Key = "environment", Value = environmentName.ToLowerInvariant() }
                    });
            }
        };
}
