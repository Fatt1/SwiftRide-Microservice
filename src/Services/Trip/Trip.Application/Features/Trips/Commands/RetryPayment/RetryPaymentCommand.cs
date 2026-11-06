using Shared.CQRS;
using Shared.Enums.Payments;

namespace Trip.Application.Features.Trips.Commands.RetryPayment;

public sealed record RetryPaymentCommand(
    Guid TripId,
    PaymentMethod PaymentMethod = PaymentMethod.Wallet,
    string? GatewayToken = null
) : ICommand;

