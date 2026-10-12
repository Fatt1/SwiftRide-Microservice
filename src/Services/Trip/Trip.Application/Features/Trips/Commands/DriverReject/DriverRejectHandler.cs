using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.DriverReject;

internal sealed class DriverRejectHandler(ITripRepository tripRepository) 
    : ICommandHandler<DriverRejectCommand>
{
    public async Task<Result> Handle(DriverRejectCommand request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
        {
            return Result.Failure(new NotFoundError("Trip", request.TripId));
        }

        if (trip.Status != TripStatus.Requested)
        {
            return Result.Failure(new ConflictError($"Không thể từ chối chuyến ở trạng thái '{trip.Status}'."));
        }

        // Gọi hàm của Domain mà bạn đã tự tay viết ở Bước 1
        trip.RejectByDriver();

        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
