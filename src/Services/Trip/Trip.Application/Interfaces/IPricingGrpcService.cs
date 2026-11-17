using Trip.Application.DTOs;

namespace Trip.Application.Interfaces;

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
        double? distanceKm = null,
        int? estimatedMinutes = null,
        CancellationToken cancellationToken = default);
}
