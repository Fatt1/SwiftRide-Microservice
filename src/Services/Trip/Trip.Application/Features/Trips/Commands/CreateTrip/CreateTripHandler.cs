using Shared;
using Shared.CQRS;
using Trip.Application.Common.Interfaces;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.CreateTrip;

internal sealed class CreateTripHandler(
    ITripRepository tripRepository,
    IPricingGrpcService pricingGrpcService) 
    : ICommandHandler<CreateTripCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
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

        // 3. Gọi gRPC sang Matching Service để lấy giá cước và khoảng cách
        var (estimatedFare, distanceKm) = await pricingGrpcService.GetPricingAsync(
            trip.Id,
            trip.PickupLat,
            trip.PickupLng,
            trip.DropoffLat,
            trip.DropoffLng,
            cancellationToken);

        // 4. Cập nhật thông tin giá vào chuyến đi và lưu lại vào DB
        trip.SetPricingInfo(estimatedFare, distanceKm, quoteId: Guid.NewGuid().ToString());
        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        return Result.Success(trip.Id);
    }
}
