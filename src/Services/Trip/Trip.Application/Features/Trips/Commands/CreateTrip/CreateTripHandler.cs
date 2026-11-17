using Shared;
using Shared.CQRS;
using Trip.Application.DTOs;
using Trip.Application.Interfaces;
using Trip.Domain.Exceptions;
using Trip.Domain.Repositories;
namespace Trip.Application.Features.Trips.Commands.CreateTrip;

internal sealed class CreateTripHandler(ITripRepository tripRepository, IPricingGrpcService pricingGrpcService)
    : ICommandHandler<CreateTripCommand, CreateTripResponse>
{
    public async Task<Result<CreateTripResponse>> Handle(CreateTripCommand request, CancellationToken ct)
    {
        var trip = Domain.Entities.Trip.Create(request.RiderId, request.PickupAddress, request.DropoffAddress,
            request.PickupLat, request.PickupLng, request.DropoffLat, request.DropoffLng);
        PricingDetailsDto pricing;
        try
        {
            pricing = await pricingGrpcService.GetPricingAsync(trip.Id, trip.PickupLat, trip.PickupLng,
            trip.DropoffLat, trip.DropoffLng, request.HasToll, request.PromoCode, request.IsRaining,
            cancellationToken: ct);
        }
        catch (PricingUnavailableException ex)
        {
            return Result.Failure<CreateTripResponse>(new UnavailableError(ex.Message));
        }
        trip.SetPricingInfo(
            (decimal)pricing.FinalTotal,
            (decimal)pricing.DistanceKm,
            pricing.MatchSessionId,
            pricing.EstimatedMinutes);

        await tripRepository.CreateAsync(trip);

        return Result.Success(new CreateTripResponse(
            trip.Id,
            trip.RiderId,
            trip.Status.ToString(),
            trip.PickupAddress,
            trip.DropoffAddress,
            trip.EstimatedFare,
            trip.DistanceKm,
            trip.CreatedAt,
            pricing,
            MatchSessionId: trip.MatchSessionId));
    }
}
