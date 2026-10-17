using Shared.CQRS;
using Shared.Enums.Payments;

namespace Trip.Application.Features.Trips.Commands.DropOff;

public sealed record DropOffCommand(
    Guid TripId,
    PaymentMethod PaymentMethod = PaymentMethod.Wallet
) : ICommand;
