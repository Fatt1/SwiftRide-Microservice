using Matching.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Commands.FindDriver;

public class FindDriverCommandHandler : ICommandHandler<FindDriverCommand, FindDriverResult>
{
    private readonly IMatchingRepository _matchingRepository;
    private readonly ILogger<FindDriverCommandHandler> _logger;
    private readonly IDriverLocationRepository _driverRepository;
    private readonly int MaxDriverAttempts = 3; // Giới hạn số lần thử tìm tài xế

    public FindDriverCommandHandler(
        IMatchingRepository matchingRepository,
        IDriverLocationRepository driverRepository,
        ILogger<FindDriverCommandHandler> logger)
    {
        _matchingRepository = matchingRepository;
        _driverRepository = driverRepository;
        _logger = logger;
    }

    public async Task<Result<FindDriverResult>> Handle(FindDriverCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("BEGIN: Find driver for tripId: {TripId}", request.TripId);
        var matching = await _matchingRepository.GetByTripIdAsync(request.TripId, cancellationToken);

        if (matching == null)
        {
            _logger.LogWarning("No matching session found for tripId: {TripId}", request.TripId);
            return Result.Failure<FindDriverResult>(new NotFoundError("Trip", request.TripId));
        }

        var excludedDriverIds = matching.DriverAttempts.Select(d => d.DriverId).Distinct().ToList();

        if (excludedDriverIds.Count >= MaxDriverAttempts)
        {
            _logger.LogWarning("Maximum number of driver attempts reached for tripId: {TripId}", request.TripId);
            return Result.Failure<FindDriverResult>(new BadError("Maximum number of driver attempts reached"));
        }

        var nearestDriver = await _driverRepository.GetNearestAvailableDriverAsync(matching.PickupLat, matching.PickupLng, 5000, excludedDriverIds, cancellationToken);

        if (nearestDriver == null)
        {
            _logger.LogWarning("No available drivers found for tripId: {TripId}", request.TripId);
            return Result.Failure<FindDriverResult>(new BadError("No available drivers found"));
        }

        await _matchingRepository.AddDriverAttemptAsync(matching.Id, nearestDriver.DriverId, cancellationToken);

        _logger.LogInformation("END: Found driver {DriverId} for tripId: {TripId}", nearestDriver.DriverId, request.TripId);
        return Result.Success<FindDriverResult>(
            new FindDriverResult(
                nearestDriver.DriverId,
                nearestDriver.FullName,
                nearestDriver.Latitude,
                nearestDriver.Longitude
                )
            );


    }
}
