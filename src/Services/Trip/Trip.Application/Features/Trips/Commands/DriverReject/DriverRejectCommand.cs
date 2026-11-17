using Shared.CQRS;

namespace Trip.Application.Features.Trips.Commands.DriverReject;

public sealed record DriverRejectResponse(
    Guid TripId,
    int RejectCount,
    int MaxAllowedRejections,
    string Status,
    string Message
);

public sealed record DriverRejectCommand(
    Guid TripId,
    Guid DriverId
) : ICommand<DriverRejectResponse>;
