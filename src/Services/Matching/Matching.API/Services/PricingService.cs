using Grpc.Core;
using Matching.API.Protos;
using Matching.Application.Features.Matching.Commands.CalculatePrice;
using MediatR;
namespace Matching.API.Services;

public class PricingGrpcService : PricingService.PricingServiceBase
{
    private readonly ILogger<PricingGrpcService> _logger;
    private readonly ISender _meditor;

    public PricingGrpcService(ILogger<PricingGrpcService> logger, ISender meditor)
    {
        _logger = logger;
        _meditor = meditor;
    }

    public override async Task<PricingResponse> GetPricing(GetPricingRequest request, ServerCallContext context)
    {
        _logger.LogInformation("BEGIN: Getting pricing for request {Request}", request);

        var command = new CalculatePriceCommand(
            request.IsRaining,
            request.PromoCode,
            request.HasToll,
            request.PickupLongitude,
            request.PickupLatitude,
            request.DestinationLongitude,
            request.DestinationLatitude,
            Guid.Parse(request.TripId)
            );
        var result = await _meditor.Send(command);

        if (result.IsSuccess)
        {
            var pricing = result.Value;
            var response = new PricingResponse
            {
                DistanceFare = pricing.DistanceFare,
                TimeFare = pricing.TimeFare,
                TaxRate = pricing.TaxRate,
                BaseFare = pricing.BaseFare,
                RetentionFactor = pricing.RetentionFactor,
                FareAfterSurge = pricing.FareAfterSurge,
                FareAfterDiscount = pricing.FareAfterDiscount,
                TollFee = pricing.TollFee,
                DiscountAmount = pricing.DiscountAmount,
                TaxAmount = pricing.TaxAmount,
                FinalTotal = pricing.FinalTotal,
                DistanceKm = pricing.DistanceKm,
                EstimatedMinutes = pricing.EstimatedMinutes
            };
            foreach (var appliedSurge in pricing.AppliedSurges)
            {
                response.AppliedSurges.Add(new AppliedSurge
                {
                    Type = appliedSurge.Type.ToString(),
                    Multiplier = appliedSurge.Multiplier
                });
            }
            _logger.LogInformation("END: Successfully got pricing for request {Request}", request);
            return response;
        }
        else
        {
            _logger.LogError("ERROR: Failed to get pricing for request {Request}. Error: {Error}", request, result.Error);
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to get pricing: {result.Error}"));
        }
    }
}
