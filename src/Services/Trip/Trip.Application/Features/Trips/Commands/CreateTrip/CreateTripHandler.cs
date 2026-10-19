using Shared;
using Shared.CQRS;
using Trip.Application.Common.Interfaces;
using Trip.Application.DTOs;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.CreateTrip;

internal sealed class CreateTripHandler(
    ITripRepository tripRepository,
    IPricingGrpcService pricingGrpcService) 
    : ICommandHandler<CreateTripCommand, CreateTripResponse>
{
    public async Task<Result<CreateTripResponse>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        // 1. Tạo chuyến đi mới ở trạng thái Requested
        var trip = Domain.Entities.Trip.Create(
            request.RiderId,
            request.PickupAddress,
            request.DropoffAddress,
            request.PickupLat,
            request.PickupLng,
            request.DropoffLat,
            request.DropoffLng);

        // 2. Lưu chuyến đi vào DB SQL Server
        await tripRepository.CreateAsync(trip);

        // 3. Gọi gRPC sang Matching Service để lấy giá cước và khoảng cách (có fallback nếu Matching chưa bật)
        PricingDetailsDto? pricingDetails = null;
        decimal estimatedFare = 50000m;
        decimal distanceKm = 5.0m;
        try
        {
            pricingDetails = await pricingGrpcService.GetPricingAsync(
                trip.Id,
                trip.PickupLat,
                trip.PickupLng,
                trip.DropoffLat,
                trip.DropoffLng,
                request.HasToll,
                request.PromoCode,
                request.IsRaining,
                cancellationToken);

            estimatedFare = (decimal)pricingDetails.FinalTotal;
            distanceKm = (decimal)pricingDetails.DistanceKm;
        }
        catch
        {
            // Fallback giá mặc định nếu Matching Service chưa khởi chạy
            pricingDetails = new PricingDetailsDto(
                DistanceFare: 30000.0,
                TimeFare: 10000.0,
                TaxRate: 0.08,
                BaseFare: 10000.0,
                RetentionFactor: 0.2,
                FareAfterSurge: 50000.0,
                FareAfterDiscount: 50000.0,
                TollFee: request.HasToll ? 10000.0 : 0.0,
                AppliedSurges: new List<AppliedSurgeDto>(),
                DiscountAmount: 0.0,
                TaxAmount: 4000.0,
                FinalTotal: 50000.0,
                DistanceKm: 5.0,
                EstimatedMinutes: 15
            );
        }

        // 4. Cập nhật thông tin giá vào chuyến đi và lưu lại vào DB
        trip.SetPricingInfo(estimatedFare, distanceKm, quoteId: Guid.NewGuid().ToString());
        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        var response = new CreateTripResponse(
            trip.Id,
            trip.RiderId,
            trip.Status.ToString(),
            trip.PickupAddress,
            trip.DropoffAddress,
            trip.EstimatedFare,
            trip.DistanceKm,
            trip.CreatedAt,
            Pricing: pricingDetails
        );

        return Result.Success(response);
    }
}
