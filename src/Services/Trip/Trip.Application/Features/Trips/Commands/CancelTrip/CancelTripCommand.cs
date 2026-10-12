using Shared.CQRS;

namespace Trip.Application.Features.Trips.Commands.CancelTrip;

public sealed record CancelTripCommand(Guid TripId) : ICommand;
