using MassTransit;
using Matching.API;
using Matching.Infrastructure.Configurations;
using Matching.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;
using Xunit;

namespace Matching.IntegrationTests.Fixtures;

public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:latest")
        .Build();

    public IPublishEndpoint PublishEndpointMock { get; } = Substitute.For<IPublishEndpoint>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Replace MongoDB client & database with real Testcontainers instance
            services.RemoveAll<IMongoClient>();
            services.RemoveAll<IMongoDatabase>();
            services.RemoveAll<MongoDbSettings>();

            var connectionString = _mongo.GetConnectionString();
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("swiftride_matching_test");

            services.AddSingleton<IMongoClient>(client);
            services.AddSingleton<IMongoDatabase>(database);

            // Replace MassTransit with Mock to isolate external message broker
            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(PublishEndpointMock);

            // Remove MassTransit bus hosted service so it doesn't try to connect to RabbitMQ
            services.RemoveAll<IHostedService>();
            services.AddHostedService<MongoDbInitializer>();
        });
    }

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();

        // Ensure MongoDb indexes (e.g. 2dsphere for geospatial query) are initialized
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MongoDbInitializer>>();
        var initializer = new MongoDbInitializer(db, logger);
        await initializer.StartAsync(CancellationToken.None);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await db.DropCollectionAsync("match_sessions");
        await db.DropCollectionAsync("driver_locations");

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MongoDbInitializer>>();
        var initializer = new MongoDbInitializer(db, logger);
        await initializer.StartAsync(CancellationToken.None);
    }

    public IMongoDatabase GetDatabase()
    {
        return Services.GetRequiredService<IMongoDatabase>();
    }

    public override async ValueTask DisposeAsync()
    {
        await _mongo.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
