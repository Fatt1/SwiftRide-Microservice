using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.CancelTrip;

internal sealed class CancelTripHandler(ITripRepository tripRepository) 
    : ICommandHandler<CancelTripCommand>
{
    public async Task<Result> Handle(CancelTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
        {
            return Result.Failure(new NotFoundError("Trip", request.TripId));
        }

        // Kiểm tra xem chuyến có nằm trong trạng thái cho phép hủy không
        if (trip.Status is TripStatus.PickedUp or TripStatus.DroppedOff or TripStatus.Completed or TripStatus.Cancelled)
        {
            return Result.Failure(new ConflictError($"Không thể hủy chuyến khi đang ở trạng thái '{trip.Status}'."));
        }

        trip.Cancel();

        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
