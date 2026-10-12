using Shared.CQRS;

namespace Trip.Application.Features.Trips.Commands.DriverAccept;

public sealed record DriverAcceptCommand(
    Guid TripId,
    Guid DriverId
) : ICommand;
