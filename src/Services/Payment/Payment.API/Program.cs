using Common.Logging;
using Payment.API.Endpoints;
using Payment.Application;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using Payment.Infrastructure.Extensions;
using Payment.Infrastructure.Persistence;
using Serilog;
using Shared.Exceptions;
using Shared.Extensions;

namespace Payment.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((context, configuration) =>
            {
                Serilogger.Configure(context, configuration);
                configuration.Enrich.With(new PaymentTraceEnricher());
            });

            // Add services to the container.
            builder.Services.AddPaymentInfrastructure(builder.Configuration);
            builder.Services.AddPaymentApplication();
            builder.AddSwiftRideApiVersioning();
            builder.Services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            builder.Services.AddControllers();
            builder.Services.AddOpenApi("v1");


            // Register the global exception handler (IExceptionHandler implementation).
            builder.Services.AddExceptionHandler<PaymentRequestExceptionHandler>();
            builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();

            // Required companion for UseExceptionHandler() when using IExceptionHandler.
            builder.Services.AddProblemDetails();


            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? builder.Environment.ApplicationName;
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
            builder.Services.AddCustomOpenTelemetry(serviceName, otlpEndpoint);

            var app = builder.Build();

            // Automatically apply pending database migrations
            await app.MigrateDatabaseAsync();

            Log.Information("Starting up: Payment API");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapCustomScalarApiReference();
            }

            // Must be placed before all other middleware so exceptions from any handler are caught.
            app.UseExceptionHandler();
            // app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
            app.MapPaymentEndpoints();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            Environment.ExitCode = 1;
        }
        finally
        {
            Log.Information("Shutting down: Payment API");
            await Log.CloseAndFlushAsync();
        }
    }
}

internal sealed class PaymentTraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current is not { } activity) return;
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
    }
}

internal sealed class PaymentRequestExceptionHandler(ILogger<PaymentRequestExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
            return false;

        var detail = badRequest.InnerException is JsonException jsonException
            ? jsonException.Message : badRequest.Message;
        logger.LogWarning("Yêu cầu không hợp lệ tại {Method} {Path}: {Detail}",
            httpContext.Request.Method, httpContext.Request.Path, detail);
        await Results.Problem(statusCode: badRequest.StatusCode,
            title: "Invalid Request", detail: detail,
            instance: $"{httpContext.Request.Method} {httpContext.Request.Path}")
            .ExecuteAsync(httpContext);
        return true;
    }
}
