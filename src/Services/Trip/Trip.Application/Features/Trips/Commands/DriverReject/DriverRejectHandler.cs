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

        // Khi một tài xế từ chối nhận chuyến, chuyến đi vẫn giữ trạng thái Requested để hệ thống tiếp tục tìm tài xế khác.
        // Chuyến đi CHỈ chuyển sang trạng thái DriverRejected khi Matching Service báo vượt quá số lần tìm kiếm
        // và bắn sự kiện TripMatchingFailedEvent qua RabbitMQ (xử lý bởi TripMatchingFailedConsumer).

        return Result.Success();
    }
}
