using Trip.Application.DTOs;

namespace Trip.Application.Common.Interfaces;

public interface IPricingGrpcService
{
    Task<PricingDetailsDto> GetPricingAsync(
        Guid tripId,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng,
        bool hasToll = false,
        string? promoCode = null,
        bool isRaining = false,
        CancellationToken cancellationToken = default);
}

