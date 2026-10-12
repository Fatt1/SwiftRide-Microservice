using Shared.CQRS;

namespace Trip.Application.Features.Trips.Commands.PickUp;

public sealed record PickUpCommand(Guid TripId) : ICommand;
