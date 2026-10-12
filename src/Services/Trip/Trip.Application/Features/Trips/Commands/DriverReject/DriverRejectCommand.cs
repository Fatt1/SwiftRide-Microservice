using Shared.CQRS;

namespace Trip.Application.Features.Trips.Commands.DriverReject;

public sealed record DriverRejectCommand(
    Guid TripId,
    Guid DriverId
) : ICommand;
