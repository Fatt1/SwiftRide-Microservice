using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Matching.Application.Features.Matching.Events;

public class TripAcceptedConsumer : IConsumer<TripAcceptedEvent>
{
    // Nhớ khi log
    // Cập nhật trạng thái của chuyến đi và trạng thái của tài xế trong cơ sở dữ liệu
    private readonly IMatchingRepository _matchingRepository;
    private readonly ILogger<TripAcceptedConsumer> _logger;

    public TripAcceptedConsumer(
        IMatchingRepository matchingRepository,
        ILogger<TripAcceptedConsumer> logger)
    {
        _matchingRepository = matchingRepository;
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

        _logger.LogInformation(
            "Trip {TripId} matched with driver {DriverId}; session {SessionId}.",
            message.TripId,
            message.DriverId,
            session.Id);
    }
}
