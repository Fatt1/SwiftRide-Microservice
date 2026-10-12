using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trip.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(TripDbContext context, ILogger logger)
    {
        var sampleId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        if (await context.Trips.AnyAsync(t => t.Id == sampleId1))
        {
            return;
        }

        logger.LogInformation("Seeding initial sample trips data for testing...");

        // 1. Chuyến xe đang yêu cầu (Requested - chờ tài xế nhận)
        var trip1 = Domain.Entities.Trip.Create(
            riderId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            pickupAddress: "227 Nguyễn Văn Cừ, Phường 4, Quận 5, TP.HCM",
            dropoffAddress: "Chợ Bến Thành, Quận 1, TP.HCM",
            pickupLat: 10.762622,
            pickupLng: 106.682373,
            dropoffLat: 10.772545,
            dropoffLng: 106.698032);
        trip1.Id = sampleId1;
        trip1.SetPricingInfo(45000m, 4.5m, Guid.NewGuid().ToString());

        // 2. Chuyến xe tài xế đã nhận (DriverAccepted)
        var trip2 = Domain.Entities.Trip.Create(
            riderId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            pickupAddress: "Landmark 81, Bình Thạnh, TP.HCM",
            dropoffAddress: "Sân bay Tân Sơn Nhất, Tân Bình, TP.HCM",
            pickupLat: 10.795123,
            pickupLng: 106.721894,
            dropoffLat: 10.818463,
            dropoffLng: 106.658825);
        trip2.Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        trip2.SetPricingInfo(120000m, 8.2m, Guid.NewGuid().ToString());
        trip2.AcceptByDriver(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        // 3. Chuyến xe đã đón khách (PickedUp - đang chở khách đi)
        var trip3 = Domain.Entities.Trip.Create(
            riderId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            pickupAddress: "Đại học Bách Khoa, Quận 10, TP.HCM",
            dropoffAddress: "Nhà hát Thành phố, Quận 1, TP.HCM",
            pickupLat: 10.772120,
            pickupLng: 106.657843,
            dropoffLat: 10.776632,
            dropoffLng: 106.703215);
        trip3.Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        trip3.SetPricingInfo(60000m, 5.8m, Guid.NewGuid().ToString());
        trip3.AcceptByDriver(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        trip3.MarkPickedUp();

        // 4. Chuyến xe đã hoàn thành (Completed)
        var trip4 = Domain.Entities.Trip.Create(
            riderId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            pickupAddress: "Hồ Con Rùa, Quận 3, TP.HCM",
            dropoffAddress: "Phố đi bộ Nguyễn Huệ, Quận 1, TP.HCM",
            pickupLat: 10.782782,
            pickupLng: 106.696123,
            dropoffLat: 10.774123,
            dropoffLng: 106.704123);
        trip4.Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        trip4.SetPricingInfo(35000m, 2.5m, Guid.NewGuid().ToString());
        trip4.AcceptByDriver(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        trip4.MarkPickedUp();
        trip4.MarkDroppedOff(35000m);
        trip4.MarkCompleted();

        await context.Trips.AddRangeAsync(trip1, trip2, trip3, trip4);
        await context.SaveChangesAsync();

        logger.LogInformation("Sample trips seeded successfully: 4 sample trips created.");
    }
}
