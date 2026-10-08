using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Matching.Infrastructure.Persistence;

public static class MongoDbSeeder
{
    public static async Task SeedAsync(IMongoDatabase database, ILogger logger, CancellationToken cancellationToken = default)
    {
        await SeedDriverLocationsAsync(database, logger, cancellationToken);
        await SeedMatchSessionsAsync(database, logger, cancellationToken);
    }

    private static async Task SeedDriverLocationsAsync(IMongoDatabase database, ILogger logger, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<DriverLocation>("driver_locations");
        var count = await collection.CountDocumentsAsync(FilterDefinition<DriverLocation>.Empty, cancellationToken: cancellationToken);

        if (count > 0)
        {
            logger.LogInformation("driver_locations collection already has {Count} documents. Skipping seed.", count);
            return;
        }

        logger.LogInformation("Seeding 10 driver locations into MongoDB...");

        var drivers = new List<DriverLocation>
        {
            // 1. Nguyễn Văn An - Quận 1 (Chợ Bến Thành) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000001"),
                fullName: "Nguyễn Văn An",
                latitude: 10.772111,
                longitude: 106.698270,
                isAvailable: true),

            // 2. Trần Minh Bình - Quận 1 (Nhà thờ Đức Bà) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000002"),
                fullName: "Trần Minh Bình",
                latitude: 10.779788,
                longitude: 106.699018,
                isAvailable: true),

            // 3. Lê Hoàng Cường - Quận 1 (Phố đi bộ Nguyễn Huệ) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000003"),
                fullName: "Lê Hoàng Cường",
                latitude: 10.774160,
                longitude: 106.703210,
                isAvailable: true),

            // 4. Phạm Quốc Dũng - Quận 3 (Hồ Con Rùa) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000004"),
                fullName: "Phạm Quốc Dũng",
                latitude: 10.782670,
                longitude: 106.695950,
                isAvailable: true),

            // 5. Hoàng Gia Em - Quận 10 (Vạn Hạnh Mall) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000005"),
                fullName: "Hoàng Gia Em",
                latitude: 10.771230,
                longitude: 106.673450,
                isAvailable: true),

            // 6. Vũ Đình Phong - Bình Thạnh (Landmark 81 / Vinhomes) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000006"),
                fullName: "Vũ Đình Phong",
                latitude: 10.793820,
                longitude: 106.721540,
                isAvailable: true),

            // 7. Đặng Hữu Giang - Phú Nhuận (Phan Xích Long) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000007"),
                fullName: "Đặng Hữu Giang",
                latitude: 10.798120,
                longitude: 106.687640,
                isAvailable: true),

            // 8. Bùi Thanh Hải - Quận 5 (Chợ Lớn) - Available
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000008"),
                fullName: "Bùi Thanh Hải",
                latitude: 10.753820,
                longitude: 106.658230,
                isAvailable: true),

            // 9. Đỗ Quang Hùng - Quận 7 (Crescent Mall / Phú Mỹ Hưng) - Busy (false)
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000009"),
                fullName: "Đỗ Quang Hùng",
                latitude: 10.729510,
                longitude: 106.721730,
                isAvailable: false),

            // 10. Ngô Trọng Khang - Tân Bình (Sân bay Tân Sơn Nhất) - Offline (false)
            DriverLocation.Create(
                driverId: Guid.Parse("11111111-1111-1111-1111-000000000010"),
                fullName: "Ngô Trọng Khang",
                latitude: 10.816430,
                longitude: 106.663120,
                isAvailable: false)
        };

        await collection.InsertManyAsync(drivers, cancellationToken: cancellationToken);
        logger.LogInformation("Successfully seeded 10 driver locations.");
    }

    private static async Task SeedMatchSessionsAsync(IMongoDatabase database, ILogger logger, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<MatchSession>("match_sessions");
        var count = await collection.CountDocumentsAsync(FilterDefinition<MatchSession>.Empty, cancellationToken: cancellationToken);

        if (count > 0)
        {
            logger.LogInformation("match_sessions collection already has {Count} documents. Skipping seed.", count);
            return;
        }

        logger.LogInformation("Seeding 2 match sessions into MongoDB...");

        var now = DateTime.UtcNow;

        // Session 1: Đang tìm tài xế (Searching) gần Landmark 81
        var session1 = new MatchSession
        {
            TripId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000001"),
            RiderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-000000000001"),
            PickupLocation = MatchSession.CreatePoint(10.795220, 106.721830), // Landmark 81
            DistanceKm = 5.2,
            EstimatedMinutes = 14,
            Status = MatchSessionStatus.Searching,
            MatchedDriverId = null,
            CreatedAt = now.AddMinutes(-3),
            LastModifiedAt = now.AddMinutes(-1),
            PricingBreakdown = new PricingBreakdown
            {
                BaseFare = 12000m,
                DistanceFare = 45000m,
                TimeFare = 14000m,
                RetentionFactor = 1.0m,
                FareAfterSurge = 71000m,
                FareAfterDiscount = 61000m,
                TollFee = 0m,
                DiscountAmount = 10000m,
                TaxRate = 0.1m,
                TaxAmount = 6100m,
                FinalTotal = 67100m,
                AppliedSurges = []
            },
            DriverAttempts =
            [
                new DriverAttempt
                {
                    DriverId = Guid.Parse("11111111-1111-1111-1111-000000000006"), // Vũ Đình Phong
                    AttemptedAt = now.AddMinutes(-1)
                }
            ]
        };

        // Session 2: Đã ghép cặp thành công (Matched) gần Nhà thờ Đức Bà
        var session2 = new MatchSession
        {
            TripId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000002"),
            RiderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-000000000002"),
            PickupLocation = MatchSession.CreatePoint(10.779788, 106.699018), // Nhà thờ Đức Bà
            DistanceKm = 7.8,
            EstimatedMinutes = 22,
            Status = MatchSessionStatus.Matched,
            MatchedDriverId = Guid.Parse("11111111-1111-1111-1111-000000000002"), // Trần Minh Bình
            CreatedAt = now.AddMinutes(-10),
            LastModifiedAt = now.AddMinutes(-8),
            PricingBreakdown = new PricingBreakdown
            {
                BaseFare = 12000m,
                DistanceFare = 70000m,
                TimeFare = 22000m,
                RetentionFactor = 1.0m,
                FareAfterSurge = 124800m,
                FareAfterDiscount = 124800m,
                TollFee = 10000m,
                DiscountAmount = 0m,
                TaxRate = 0.1m,
                TaxAmount = 13480m,
                FinalTotal = 148280m,
                AppliedSurges =
                [
                    new AppliedSurge
                    {
                        Type = SurgeType.Time,
                        Name = "Peak Hour Surge",
                        Multiplier = 1.2m
                    }
                ]
            },
            DriverAttempts =
            [
                new DriverAttempt
                {
                    DriverId = Guid.Parse("11111111-1111-1111-1111-000000000002"), // Trần Minh Bình
                    AttemptedAt = now.AddMinutes(-8)
                }
            ]
        };

        await collection.InsertManyAsync([session1, session2], cancellationToken: cancellationToken);
        logger.LogInformation("Successfully seeded 2 match sessions.");
    }
}
