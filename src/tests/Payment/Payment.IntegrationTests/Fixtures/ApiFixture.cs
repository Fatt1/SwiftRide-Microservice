using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Payment.API;
using Payment.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Payment.IntegrationTests.Fixtures;

public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:latest")
        .Build();

    public IPublishEndpoint PublishEndpointMock { get; } = Substitute.For<IPublishEndpoint>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            // Replace PostgreSQL DbContext with Testcontainers instance
            services.RemoveAll<DbContextOptions<PaymentDbContext>>();
            services.AddDbContext<PaymentDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString(), npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName);
                });
            });

            // Replace MassTransit with Mock to isolate external RabbitMQ message broker
            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(PublishEndpointMock);

            // Remove MassTransit bus hosted service so it doesn't try to connect to RabbitMQ
            services.RemoveAll<IHostedService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply EF migrations to initialize schema
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"LedgerEntries\", \"Refunds\", \"Payments\", \"Wallets\" CASCADE;");

        PublishEndpointMock.ClearReceivedCalls();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test DbContext instance created for test assertions")]
    public PaymentDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
