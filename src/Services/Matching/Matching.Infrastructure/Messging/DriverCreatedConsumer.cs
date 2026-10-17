using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Matching.Infrastructure.Consumers;

public class DriverCreatedConsumer : IConsumer<DriverCreatedEvent>
{
    private readonly IDriverLocationRepository _driverLocationRepository;
    private readonly ILogger<DriverCreatedConsumer> _logger;

    public DriverCreatedConsumer(
        IDriverLocationRepository driverLocationRepository,
        ILogger<DriverCreatedConsumer> logger)
    {
        _driverLocationRepository = driverLocationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Nhận DriverCreatedEvent: DriverId={DriverId}, FullName={FullName}, Email={Email}.",
            message.DriverId,
            message.FullName,
            message.Email);

        if (message.DriverId == Guid.Empty)
        {
            _logger.LogWarning("Bỏ qua DriverCreatedEvent vì DriverId rỗng.");
            return;
        }

        var existingDriver = await _driverLocationRepository.GetByDriverIdAsync(message.DriverId, context.CancellationToken);
        if (existingDriver != null)
        {
            _logger.LogInformation("Driver {DriverId} đã tồn tại trong database Matching. Bỏ qua.", message.DriverId);
            return;
        }

        // Thêm đại một tọa độ vào database (Khu vực trung tâm TP.HCM, Quận 1)
        // Dao động nhẹ xung quanh tọa độ (10.772, 106.698) để các tài xế không trùng khít 1 điểm
        var random = Random.Shared;
        var latitude = 10.760000 + random.NextDouble() * (10.785000 - 10.760000);
        var longitude = 106.680000 + random.NextDouble() * (106.710000 - 106.680000);

        var driverLocation = DriverLocation.Create(
            driverId: message.DriverId,
            fullName: string.IsNullOrWhiteSpace(message.FullName) ? "Tài xế mới" : message.FullName,
            latitude: latitude,
            longitude: longitude,
            isAvailable: true);

        await _driverLocationRepository.CreateAsync(driverLocation, context.CancellationToken);

        _logger.LogInformation(
            "Đã thêm tài xế {DriverId} ({FullName}) vào Matching DB với tọa độ ({Latitude:F6}, {Longitude:F6}).",
            message.DriverId,
            driverLocation.FullName,
            latitude,
            longitude);
    }
}
