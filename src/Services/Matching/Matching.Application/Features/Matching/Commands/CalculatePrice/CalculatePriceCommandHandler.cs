using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Application.Helpers;
using Matching.Application.Pricing.Stategies;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.CQRS;

namespace Matching.Application.Features.Matching.Commands.CalculatePrice;

public class CalculatePriceCommandHandler(IMatchingRepository matchingRepository, ILogger<CalculatePriceCommandHandler> logger, IOptions<PricingConfig> config)
    : ICommandHandler<CalculatePriceCommand, PricingBreakdown>
{

    public async Task<Result<PricingBreakdown>> Handle(CalculatePriceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("BEGIN: Calculating price for trip {TripId} and rider {RiderId}", request.TripId, request.RiderId);

        var (distance, estimatedTime, duration) = GeoHelper.CalculateTravelEstimate(request.PickupLatitude, request.PickupLongitude, request.DestinationLatitude, request.DestinationLongitude);

        var pricingContext = new PricingContext(
            distance,
            estimatedTime,
            DateTime.UtcNow,
            request.IsRaining,
            request.PromoCode,
            request.HasToll
            );

        // Tính toán giá dựa trên các chiến lược định giá
        IPricingStrategy standardPricing = new StandardPricing(config.Value);
        var timePricing = new TimeSurgePricing(standardPricing, config.Value);
        var weatherSurgePricing = new WeatherSurgePricing(timePricing, config.Value);
        var tollPricing = new TollPricing(weatherSurgePricing, config.Value);
        var promoPricing = new PromotionPricing(tollPricing, config.Value);

        var pricingBreakdown = promoPricing.GetPrice(pricingContext);
        var taxAmount = pricingBreakdown.FinalTotal * config.Value.TaxRate;

        // Tính thuế và cập nhật giá trị cuối cùng
        pricingBreakdown.FinalTotal = Math.Round(pricingBreakdown.FinalTotal + taxAmount, 2);
        pricingBreakdown.TaxAmount = taxAmount;
        pricingBreakdown.TaxRate = config.Value.TaxRate;

        await matchingRepository.CreateAsync(
            new MatchSession
            {
                TripId = request.TripId,
                RiderId = request.RiderId,
                PickupLocation = MatchSession.CreatePoint(request.PickupLatitude, request.PickupLongitude),
                DistanceKm = distance,
                EstimatedMinutes = estimatedTime,
                PricingBreakdown = pricingBreakdown,
                Status = Domain.Enums.MatchSessionStatus.Searching,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            }, cancellationToken
            );

        logger.LogInformation("END: Calculating price for trip {TripId} and rider {RiderId}. Final price: {FinalPrice}", request.TripId, request.RiderId, pricingBreakdown.FinalTotal);

        return Result<PricingBreakdown>.Success(pricingBreakdown);

    }
}
