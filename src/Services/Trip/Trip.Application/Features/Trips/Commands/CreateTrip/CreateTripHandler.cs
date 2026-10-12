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
        decimal estimatedFare = 50000m;
        decimal distanceKm = 5.0m;
        try
        {
            var pricing = await pricingGrpcService.GetPricingAsync(
                trip.Id,
                trip.PickupLat,
                trip.PickupLng,
                trip.DropoffLat,
                trip.DropoffLng,
                cancellationToken);
            estimatedFare = pricing.FinalTotal;
            distanceKm = pricing.DistanceKm;
        }
        catch
        {
            // Fallback giá mặc định nếu Matching Service chưa khởi chạy
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
            trip.CreatedAt
        );

        return Result.Success(response);
    }
}
