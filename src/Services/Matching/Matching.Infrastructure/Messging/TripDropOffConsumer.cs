using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Matching.Infrastructure.Consumers;

public class TripDropOffConsumer : IConsumer<TripDropOffEvent>
{
    private readonly IDriverLocationRepository _driverLocationRepository;
    private readonly ILogger<TripDropOffConsumer> _logger;

    public TripDropOffConsumer(
        IDriverLocationRepository driverLocationRepository,
        ILogger<TripDropOffConsumer> logger)
    {
        _driverLocationRepository = driverLocationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TripDropOffEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Received TripDropOffEvent for TripId={TripId}, DriverId={DriverId}, Amount={Amount}.",
            message.TripId,
            message.DriverId,
            message.Amount);

        if (message.DriverId == Guid.Empty)
        {
            _logger.LogWarning("TripDropOffEvent has empty DriverId. Skipping.");
            return;
        }

        var driver = await _driverLocationRepository.GetByDriverIdAsync(message.DriverId, context.CancellationToken);
        if (driver == null)
        {
            _logger.LogWarning("Driver {DriverId} not found in Matching DB when processing TripDropOffEvent.", message.DriverId);
            return;
        }

        // Cập nhật lại trạng thái khả dụng cho tài xế để có thể tiếp tục nhận cuốc mới
        driver.SetAvailability(true);
        await _driverLocationRepository.UpdateAsync(driver, context.CancellationToken);

        _logger.LogInformation(
            "Driver {DriverId} ({FullName}) availability reset to true. Ready for new trips.",
            message.DriverId,
            driver.FullName);
    }
}
