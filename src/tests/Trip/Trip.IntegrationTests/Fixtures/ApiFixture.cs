using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.MsSql;
using Trip.API;
using Trip.Application.Common.Interfaces;
using Trip.Infrastructure.Persistence;
using Xunit;

namespace Trip.IntegrationTests.Fixtures;

public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _mssql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public IPublishEndpoint PublishEndpointMock { get; } = Substitute.For<IPublishEndpoint>();
    public IPricingGrpcService PricingGrpcServiceMock { get; } = Substitute.For<IPricingGrpcService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Replace SQL Server DbContext with Testcontainers instance
            services.RemoveAll<DbContextOptions<TripDbContext>>();
            services.AddDbContext<TripDbContext>(options =>
            {
                options.UseSqlServer(_mssql.GetConnectionString(), sql =>
                {
                    sql.MigrationsAssembly(typeof(TripDbContext).Assembly.FullName);
                });
            });

            // Replace MassTransit with Mock to isolate external RabbitMQ message broker
            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(PublishEndpointMock);

            // Remove MassTransit bus hosted service so it doesn't try to connect to RabbitMQ
            services.RemoveAll<IHostedService>();

            // Replace gRPC pricing service with Mock for controlled test responses
            services.RemoveAll<IPricingGrpcService>();
            services.AddSingleton(PricingGrpcServiceMock);
        });
    }

    public async Task InitializeAsync()
    {
        await _mssql.StartAsync();

        // Apply EF migrations to initialize schema
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TripDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TripDbContext>();
        db.Trips.RemoveRange(db.Trips);
        await db.SaveChangesAsync();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test DbContext instance created for test assertions")]
    public TripDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TripDbContext>();
    }

    public override async ValueTask DisposeAsync()
    {
        await _mssql.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
