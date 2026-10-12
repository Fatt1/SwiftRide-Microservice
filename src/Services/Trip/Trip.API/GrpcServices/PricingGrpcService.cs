using Matching.API.Protos;
using Microsoft.Extensions.Logging;
using Trip.Application.Common.Interfaces;

namespace Trip.API.GrpcServices;

public class PricingGrpcService : IPricingGrpcService
{
    private readonly PricingService.PricingServiceClient _pricingClient;
    private readonly ILogger<PricingGrpcService> _logger;

    public PricingGrpcService(
        PricingService.PricingServiceClient pricingClient,
        ILogger<PricingGrpcService> logger)
    {
        _pricingClient = pricingClient ?? throw new ArgumentNullException(nameof(pricingClient));
        _logger = logger;
    }

    public async Task<(decimal FinalTotal, decimal DistanceKm)> GetPricingAsync(
        Guid tripId,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("BEGIN: GetPricing via gRPC for TripId: {TripId}", tripId);

            var request = new GetPricingRequest
            {
                TripId = tripId.ToString(),
                PickupLatitude = pickupLat,
                PickupLongitude = pickupLng,
                DestinationLatitude = dropoffLat,
                DestinationLongitude = dropoffLng,
                HasToll = false,
                IsRaining = false,
                PromoCode = string.Empty
            };

            var response = await _pricingClient.GetPricingAsync(request, cancellationToken: cancellationToken);

            _logger.LogInformation("END: Successfully got pricing for TripId {TripId}: Fare = {Fare}, Distance = {Distance}km",
                tripId, response.FinalTotal, response.DistanceKm);

            return ((decimal)response.FinalTotal, (decimal)response.DistanceKm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Failed to call Pricing gRPC for TripId: {TripId}", tripId);
            throw;
        }
    }
}
