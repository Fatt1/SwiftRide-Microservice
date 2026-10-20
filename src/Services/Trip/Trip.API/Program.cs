using Common.Logging;
using Serilog;
using Shared.Exceptions;
using Shared.Extensions;
using Trip.API.Endpoints;
using Trip.Application.Extensions;
using Trip.Infrastructure.Extensions;
using Trip.Infrastructure.Persistence;

namespace Trip.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog(Serilogger.Configure);

            // Add services to the container.
            builder.Services.AddTripApplication();
            builder.Services.AddTripInfrastructure(builder.Configuration);

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi("v1");


            // Register the global exception handler (IExceptionHandler implementation).
            builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();

            // Required companion for UseExceptionHandler() when using IExceptionHandler.
            builder.Services.AddProblemDetails();


            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? builder.Environment.ApplicationName;
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
            builder.Services.AddCustomOpenTelemetry(serviceName, otlpEndpoint);

            builder.AddSwiftRideApiVersioning();


            var app = builder.Build();

            // Automatically apply pending database migrations
            await app.MigrateDatabaseAsync();

            Log.Information("Starting up: Trip API");

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

            app.MapGet("/", () => Results.Redirect("/scalar/v1"));
            app.MapTripEndpoints();

            await app.RunAsync();
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("Shutting down: Trip API");
            await Log.CloseAndFlushAsync();
        }
    }
}
