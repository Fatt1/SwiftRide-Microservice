using Shared.CQRS;

namespace Matching.Application.Features.Matching.Commands.FindDriver;

public record FindDriverCommand(
    Guid TripId,
    string? PickupAddress = null,
    string? DropoffAddress = null,
    Guid? RiderId = null) : ICommand<FindDriverResult>;

public record FindDriverResult(Guid DriverId, string FullName, double Latitude, double Longitude);

