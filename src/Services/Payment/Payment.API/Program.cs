using Common.Logging;
using Payment.Infrastructure.Extensions;
using Payment.Infrastructure.Persistence;
using Serilog;
using Shared.Exceptions;
using Shared.Extensions;
using Payment.Application;
using Payment.Application.Payments;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Payment.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog(Serilogger.Configure);

            // Add services to the container.
            builder.Services.AddPaymentInfrastructure(builder.Configuration);
            builder.Services.AddPaymentApplication();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi("v1");


            // Register the global exception handler (IExceptionHandler implementation).
            builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();

            // Required companion for UseExceptionHandler() when using IExceptionHandler.
            builder.Services.AddProblemDetails();


            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? builder.Environment.ApplicationName;
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
            builder.Services.AddCustomOpenTelemetry(serviceName, otlpEndpoint);
            builder.Services.AddOpenTelemetry()
                .WithTracing(t => t.AddSource(PaymentTelemetry.Name))
                .WithMetrics(m => m.AddMeter(PaymentTelemetry.Name));

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


            app.MapControllers();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("Shutting down: Payment API");
            await Log.CloseAndFlushAsync();
        }
    }
}
