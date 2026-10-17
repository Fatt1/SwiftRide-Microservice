using Shared.CQRS;
using Trip.Application.DTOs;

namespace Trip.Application.Features.Trips.Commands.CreateTrip;

public sealed record CreateTripCommand(
    Guid RiderId,
    string PickupAddress,
    string DropoffAddress,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng,
    bool HasToll = false,
    string? PromoCode = null,
    bool IsRaining = false
) : ICommand<CreateTripResponse>;
