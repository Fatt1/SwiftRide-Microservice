namespace Trip.Application.Common.Interfaces;

public interface IPricingGrpcService
{
    Task<(decimal FinalTotal, decimal DistanceKm)> GetPricingAsync(
        Guid tripId,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng,
        CancellationToken cancellationToken = default);
}
