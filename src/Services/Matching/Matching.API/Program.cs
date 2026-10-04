
using Common.Logging;
using Matching.Infrastructure;
using Serilog;
using Shared.Exceptions;
using Shared.Extensions;

namespace Matching.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog(Serilogger.Configure);

            // Add services to the container.
            builder.Services.AddMatchingInfrastructure(builder.Configuration);

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi("v1");


            // Register the global exception handler (IExceptionHandler implementation).
            builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();

            // Required companion for UseExceptionHandler() when using IExceptionHandler.
            builder.Services.AddProblemDetails();


            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? builder.Environment.ApplicationName;
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
            builder.Services.AddCustomOpenTelemetry(serviceName, otlpEndpoint);

            var app = builder.Build();

            Log.Information("Starting up: Matching API");

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

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("Shutting down: Matching API");
            await Log.CloseAndFlushAsync();
        }

    }
}
