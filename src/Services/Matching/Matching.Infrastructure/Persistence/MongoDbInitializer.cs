using Matching.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Matching.Infrastructure.Persistence;

public class MongoDbInitializer : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(IMongoDatabase database, ILogger<MongoDbInitializer> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing MongoDB collections and indexes...");

            // 1. DriverLocation indexes
            var driverLocations = _database.GetCollection<DriverLocation>("driver_locations");
            var driverIdIndex = new CreateIndexModel<DriverLocation>(
                Builders<DriverLocation>.IndexKeys.Ascending(x => x.DriverId),
                new CreateIndexOptions { Unique = true, Name = "IX_DriverLocation_DriverId" });
            var availabilityIndex = new CreateIndexModel<DriverLocation>(
                Builders<DriverLocation>.IndexKeys.Ascending(x => x.IsAvailable),
                new CreateIndexOptions { Name = "IX_DriverLocation_IsAvailable" });

            var indexKeys = Builders<DriverLocation>.IndexKeys.Geo2DSphere(x => x.Location);
            await driverLocations.Indexes.CreateOneAsync(new CreateIndexModel<DriverLocation>(indexKeys, new CreateIndexOptions
            {
                Name = "ix_pickup_location_2dsphere"
            }));

            await driverLocations.Indexes.CreateManyAsync([driverIdIndex, availabilityIndex], cancellationToken);

            // 2. MatchSession indexes
            var matchSessions = _database.GetCollection<MatchSession>("match_sessions");
            var tripIdIndex = new CreateIndexModel<MatchSession>(
                Builders<MatchSession>.IndexKeys.Ascending(x => x.TripId),
                new CreateIndexOptions { Name = "IX_MatchSession_TripId" });
            await matchSessions.Indexes.CreateOneAsync(tripIdIndex, cancellationToken: cancellationToken);

            _logger.LogInformation("MongoDB initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing MongoDB.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
