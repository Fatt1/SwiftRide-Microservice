using Matching.Domain.Entities;
using Matching.Domain.Enums;
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
            _logger.LogInformation("Initializing MongoDB collections, indexes, and seed data...");

            // 1. DriverLocation indexes
            var driverLocations = _database.GetCollection<DriverLocation>("driver_locations");
            var driverIdIndex = new CreateIndexModel<DriverLocation>(
                Builders<DriverLocation>.IndexKeys.Ascending(x => x.DriverId),
                new CreateIndexOptions { Unique = true, Name = "IX_DriverLocation_DriverId" });
            var availabilityIndex = new CreateIndexModel<DriverLocation>(
                Builders<DriverLocation>.IndexKeys.Ascending(x => x.IsAvailable),
                new CreateIndexOptions { Name = "IX_DriverLocation_IsAvailable" });

            await driverLocations.Indexes.CreateManyAsync([driverIdIndex, availabilityIndex], cancellationToken);

            // 2. MatchSession indexes
            var matchSessions = _database.GetCollection<MatchSession>("match_sessions");
            var tripIdIndex = new CreateIndexModel<MatchSession>(
                Builders<MatchSession>.IndexKeys.Ascending(x => x.TripId),
                new CreateIndexOptions { Name = "IX_MatchSession_TripId" });
            await matchSessions.Indexes.CreateOneAsync(tripIdIndex, cancellationToken: cancellationToken);

            // 3. Seed PricingConfig if empty
            var pricingConfigs = _database.GetCollection<PricingConfig>("pricing_configs");
            var hasConfig = await pricingConfigs.Find(_ => true).AnyAsync(cancellationToken);
            if (!hasConfig)
            {
                var defaultConfig = new PricingConfig
                {
                    Id = Guid.CreateVersion7(),
                    BaseFare = 15000,
                    PerKmRate = 8000,
                    PerMinRate = 500,
                    TaxRate = 0.10m,
                    SurgeApplyMode = SurgeApplyMode.Multiply,
                    SurgeRules =
                    [
                        new SurgeRule
                        {
                            RuleId = Guid.NewGuid(),
                            Type = SurgeType.Time,
                            Name = "Peak Hour (Morning)",
                            Multiplier = 1.3m,
                            Condition = new SurgeCondition { FromHour = 7, ToHour = 9 }
                        },
                        new SurgeRule
                        {
                            RuleId = Guid.NewGuid(),
                            Type = SurgeType.Time,
                            Name = "Peak Hour (Evening)",
                            Multiplier = 1.3m,
                            Condition = new SurgeCondition { FromHour = 17, ToHour = 19 }
                        }
                    ],
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await pricingConfigs.InsertOneAsync(defaultConfig, cancellationToken: cancellationToken);
                _logger.LogInformation("Seeded default PricingConfig successfully.");
            }

            _logger.LogInformation("MongoDB initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing MongoDB.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
