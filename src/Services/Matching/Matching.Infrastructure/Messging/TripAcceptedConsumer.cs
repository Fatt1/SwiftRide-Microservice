using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Matching.Infrastructure.Consumers;

public class TripAcceptedConsumer : IConsumer<TripAcceptedEvent>
{
    private readonly IMatchingRepository _matchingRepository;
    private readonly IDriverLocationRepository _driverLocationRepository;
    private readonly ILogger<TripAcceptedConsumer> _logger;

    public TripAcceptedConsumer(
        IMatchingRepository matchingRepository,
        IDriverLocationRepository driverLocationRepository,
        ILogger<TripAcceptedConsumer> logger)
    {
        _matchingRepository = matchingRepository;
        _driverLocationRepository = driverLocationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TripAcceptedEvent> context)
    {

        _logger.LogInformation(
            "Received TripAcceptedEvent for trip {TripId} and driver {DriverId}.",
            context.Message.TripId,
            context.Message.DriverId);
        var message = context.Message;
        var ct = context.CancellationToken;

        if (message.TripId == Guid.Empty || message.DriverId == Guid.Empty)
            throw new ArgumentException(
                "TripAcceptedEvent must contain a trip id and driver id.");

        var session = await _matchingRepository.GetByTripIdAsync(
            message.TripId,
            ct);

        if (session.Status is MatchSessionStatus.NoDriver
            or MatchSessionStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Match session '{session.Id}' is already {session.Status}.");
        }

        if (session.MatchedDriverId is Guid currentDriverId
            && currentDriverId != message.DriverId)
        {
            throw new InvalidOperationException(
                $"Trip '{message.TripId}' is already matched " +
                $"with driver '{currentDriverId}'.");
        }

        // Event gửi lại với cùng tài xế: không cần lưu lần nữa.
        if (session.Status == MatchSessionStatus.Matched
            && session.MatchedDriverId == message.DriverId)
        {
            _logger.LogInformation(
                "Trip {TripId} was already matched with driver {DriverId}.",
                message.TripId,
                message.DriverId);
            return;
        }

        session.MarkMatched(message.DriverId);
        await _matchingRepository.UpdateAsync(session, ct);

        // Cập nhật trạng thái tài xế sang bận (đang thực hiện chuyến đi)
        var driver = await _driverLocationRepository.GetByDriverIdAsync(message.DriverId, ct);
        if (driver != null)
        {
            driver.SetAvailability(false);
            await _driverLocationRepository.UpdateAsync(driver, ct);
            _logger.LogInformation("Driver {DriverId} availability set to false (in trip).", message.DriverId);
        }

        _logger.LogInformation(
            "Trip {TripId} matched with driver {DriverId}; session {SessionId}.",
            message.TripId,
            message.DriverId,
            session.Id);
    }
}
