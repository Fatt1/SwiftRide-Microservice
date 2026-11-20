using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
using Shared;
using Shared.CQRS;
namespace Matching.Application.Features.Matching.Commands.FindDriver;

public sealed class FindDriverCommandHandler(IMatchingRepository matchingRepository,
    IDriverLocationRepository driverRepository, IPublishEndpoint publisher)
    : ICommandHandler<FindDriverCommand, FindDriverResult>
{
    public async Task<Result<FindDriverResult>> Handle(FindDriverCommand request, CancellationToken ct)
    {
        // Validation and all matching decisions use Matching's own MongoDB only.
        var session = await matchingRepository.GetByTripIdAsync(request.TripId, ct);
        if (session is null)
            return Result.Failure<FindDriverResult>(new NotFoundError("MatchSession for trip", request.TripId));

        if (session.Status is not MatchSessionStatus.Searching)
            return Result.Failure<FindDriverResult>(new ConflictError("Matching has already ended."));

        if (session.Status == MatchSessionStatus.NoDriver)
            return Result.Failure<FindDriverResult>(new BadError("Matching has ended without a driver."));

        // Nếu tài xế đã được đề xuất mà chưa chấp nhận hoặc từ chối chuyến, không tìm kiếm lại.
        if (session.OfferedDriverId.HasValue)
        {
            return Result.Failure<FindDriverResult>(new ConflictError($"Driver with ID {session.OfferedDriverId.Value} has already been offered for this trip. "));
        }


        var excluded = session.DriverAttempts.Select(x => x.DriverId).Distinct().ToList();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var driver = await driverRepository.GetNearestAvailableDriverAsync(session.PickupLat, session.PickupLng, 5000, excluded, ct);
            if (driver == null)
                return Result.Failure<FindDriverResult>(new BadError("No available drivers within the search radius."));

            if (!await driverRepository.TryReserveAsync(driver.DriverId, session.TripId, ct))
            {
                // Another trip took this candidate. Continue with the next nearest driver.
                excluded.Add(driver.DriverId);
                continue;
            }

            // Only a successfully reserved driver becomes an invitation/attempt.
            if (!session.OfferDriver(driver.DriverId))
            {
                await driverRepository.ReleaseAsync(driver.DriverId, session.TripId, ct);
                return Result.Failure<FindDriverResult>(new ConflictError("Matching has already ended."));
            }
            await matchingRepository.UpdateAsync(session, ct);
            return await PublishOfferAsync(session, ct);
        }
    }

    private async Task<Result<FindDriverResult>> PublishOfferAsync(MatchSession session, CancellationToken ct)
    {
        var driver = await driverRepository.GetByDriverIdAsync(session.OfferedDriverId!.Value, ct);
        if (driver == null)
            return Result.Failure<FindDriverResult>(new ConflictError("Offered driver no longer exists."));

        // Publish only: Trip consumes and validates the invitation independently.
        // Repeating Matching republishes the same offer, without another attempt.
        await publisher.Publish(new DriverFoundEvent
        {
            TripId = session.TripId,
            MatchSessionId = session.Id,
            DriverId = driver.DriverId,
            DriverName = driver.FullName,
            MaxAllowedRejections = session.MaxAllowedRejections,
            TotalFare = (decimal)session.PricingBreakdown.FinalTotal,
            DistanceKm = Math.Round(session.DistanceKm, 1, MidpointRounding.AwayFromZero),
            EstimatedDurationMinutes = session.EstimatedMinutes
        }, ct);
        return Result.Success(new FindDriverResult(driver.DriverId, driver.FullName, driver.Latitude,
            driver.Longitude, session.Id));
    }

}
