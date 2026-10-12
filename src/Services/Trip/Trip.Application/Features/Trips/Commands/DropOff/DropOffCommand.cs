using Shared.CQRS;
using Shared.Enums.Payments;

namespace Trip.Application.Features.Trips.Commands.DropOff;

public sealed record DropOffCommand(
    Guid TripId,
    decimal FinalFare,
    PaymentMethod PaymentMethod = PaymentMethod.Wallet
) : ICommand;
