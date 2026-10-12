using Shared.CQRS;

namespace Trip.Application.Features.Trips.Commands.CreateTrip;

public sealed record CreateTripCommand(
    Guid RiderId,
    string PickupAddress,
    string DropoffAddress,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng
) : ICommand<Guid>;
