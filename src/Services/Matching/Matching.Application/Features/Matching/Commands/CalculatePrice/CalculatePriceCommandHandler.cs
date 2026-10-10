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
    : ICommandHandler<CalculatePriceCommand, CalculatePriceDto>
{

    public async Task<Result<CalculatePriceDto>> Handle(CalculatePriceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("BEGIN: Calculating price for trip {TripId}", request.TripId);

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
        var taxAmount = Math.Round(pricingBreakdown.FinalTotal * config.Value.TaxRate, 2);

        
        pricingBreakdown.DistanceFare = Math.Round(pricingBreakdown.DistanceFare, 2);
        pricingBreakdown.TimeFare = Math.Round(pricingBreakdown.TimeFare, 2);
        pricingBreakdown.BaseFare = Math.Round(pricingBreakdown.BaseFare, 2);
        pricingBreakdown.FareAfterSurge = Math.Round(pricingBreakdown.FareAfterSurge, 2);
        pricingBreakdown.FareAfterDiscount = Math.Round(pricingBreakdown.FareAfterDiscount, 2);
        pricingBreakdown.DiscountAmount = Math.Round(pricingBreakdown.DiscountAmount, 2);
        pricingBreakdown.TollFee = Math.Round(pricingBreakdown.TollFee, 2);
        pricingBreakdown.TaxAmount = taxAmount;
        pricingBreakdown.TaxRate = config.Value.TaxRate;

        // Chỉ làm tròn duy nhất FinalTotal theo tiền VND (ví dụ: 67.891đ -> 68.000đ)
        pricingBreakdown.FinalTotal = CurrencyHelper.RoundVnd(pricingBreakdown.FinalTotal + taxAmount);

        await matchingRepository.CreateAsync(
            new MatchSession
            {
                TripId = request.TripId,
                PickupLocation = MatchSession.CreatePoint(request.PickupLatitude, request.PickupLongitude),
                DistanceKm = distance,
                EstimatedMinutes = estimatedTime,
                PricingBreakdown = pricingBreakdown,
                Status = Domain.Enums.MatchSessionStatus.Searching,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            }, cancellationToken
            );

        logger.LogInformation("END: Calculating price for trip {TripId}. Final price: {FinalPrice}", request.TripId, pricingBreakdown.FinalTotal);

        var responseDto = new CalculatePriceDto
        {
            DistanceKm = distance,
            EstimatedMinutes = estimatedTime,
            DistanceFare = pricingBreakdown.DistanceFare,
            TimeFare = pricingBreakdown.TimeFare,
            TaxRate = pricingBreakdown.TaxRate,
            BaseFare = pricingBreakdown.BaseFare,
            RetentionFactor = pricingBreakdown.RetentionFactor,
            FareAfterSurge = pricingBreakdown.FareAfterSurge,
            FareAfterDiscount = pricingBreakdown.FareAfterDiscount,
            TollFee = pricingBreakdown.TollFee,
            AppliedSurges = pricingBreakdown.AppliedSurges,
            DiscountAmount = pricingBreakdown.DiscountAmount,
            TaxAmount = pricingBreakdown.TaxAmount,
            FinalTotal = pricingBreakdown.FinalTotal
        };

        return Result<CalculatePriceDto>.Success(responseDto);

    }
}
