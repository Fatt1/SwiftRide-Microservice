using Matching.API.Protos;
using Microsoft.Extensions.Logging;
using Trip.Application.Common.Interfaces;
using Trip.Application.DTOs;

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

    public async Task<PricingDetailsDto> GetPricingAsync(
        Guid tripId,
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng,
        bool hasToll = false,
        string? promoCode = null,
        bool isRaining = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("BEGIN: GetPricing via gRPC for TripId: {TripId}, HasToll: {HasToll}, IsRaining: {IsRaining}, PromoCode: {PromoCode}",
                tripId, hasToll, isRaining, promoCode ?? "None");

            var request = new GetPricingRequest
            {
                TripId = tripId.ToString(),
                PickupLatitude = pickupLat,
                PickupLongitude = pickupLng,
                DestinationLatitude = dropoffLat,
                DestinationLongitude = dropoffLng,
                HasToll = hasToll,
                IsRaining = isRaining,
                PromoCode = promoCode ?? string.Empty
            };

            var response = await _pricingClient.GetPricingAsync(request, cancellationToken: cancellationToken);

            _logger.LogInformation("END: Successfully got pricing for TripId {TripId}: FinalTotal = {FinalTotal}, Distance = {Distance}km",
                tripId, response.FinalTotal, response.DistanceKm);

            var appliedSurges = new List<AppliedSurgeDto>();
            if (response.AppliedSurges != null)
            {
                foreach (var surge in response.AppliedSurges)
                {
                    appliedSurges.Add(new AppliedSurgeDto(surge.Type, surge.Name, surge.Multiplier));
                }
            }

            return new PricingDetailsDto(
                DistanceFare: response.DistanceFare,
                TimeFare: response.TimeFare,
                TaxRate: response.TaxRate,
                BaseFare: response.BaseFare,
                RetentionFactor: response.RetentionFactor,
                FareAfterSurge: response.FareAfterSurge,
                FareAfterDiscount: response.FareAfterDiscount,
                TollFee: response.TollFee,
                AppliedSurges: appliedSurges,
                DiscountAmount: response.DiscountAmount,
                TaxAmount: response.TaxAmount,
                FinalTotal: response.FinalTotal,
                DistanceKm: response.DistanceKm,
                EstimatedMinutes: response.EstimatedMinutes
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR: Failed to call Pricing gRPC for TripId: {TripId}", tripId);
            throw;
        }
    }
}
