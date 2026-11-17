using Matching.Application.Features.Matching.Dtos;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Commands.CalculatePrice;

public record CalculatePriceCommand(
        bool IsRaining,
        string? PromoCode,
        bool HasToll,
        double PickupLongitude,
        double PickupLatitude,
        double DestinationLongitude,
        double DestinationLatitude,
        Guid TripId) : ICommand<CalculatePriceDto>;
