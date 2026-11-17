using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
namespace Trip.Application.Features.Trips.Commands.DriverReject;

internal sealed class DriverRejectHandler(ITripRepository tripRepository, IPublishEndpoint publishEndpoint)
    : ICommandHandler<DriverRejectCommand, DriverRejectResponse>
{
    public async Task<Result<DriverRejectResponse>> Handle(DriverRejectCommand request, CancellationToken ct)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
            return Result.Failure<DriverRejectResponse>(new NotFoundError("Trip", request.TripId));

        if (trip.Status != TripStatus.Requested)
            return Result.Failure<DriverRejectResponse>(new ConflictError($"Cannot reject a trip in status {trip.Status}."));
        if (trip.OfferedDriverId != request.DriverId)
            return Result.Failure<DriverRejectResponse>(new ConflictError("Only the current offered driver can reject this trip."));

        trip.RejectByDriver(request.DriverId);
        await tripRepository.Update(trip);
        await publishEndpoint.Publish(new DriverRejectEvent
        {
            TripId = trip.Id,
            DriverId = request.DriverId,
            RiderId = trip.RiderId,
            MatchSessionId = trip.MatchSessionId!,
            RejectCount = trip.RejectCount,
            TripFailed = trip.Status == TripStatus.Failed,
            Reason = trip.FailureReason ?? "Driver rejected the trip."
        }, ct);
        await tripRepository.SaveChangesAsync();
        return Result.Success(new DriverRejectResponse(trip.Id, trip.RejectCount, trip.MaxAllowedRejections, trip.Status.ToString(),
             "Hủy chuyến thành công."));
    }
}
