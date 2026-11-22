using Shared.CQRS;
using Shared.Enums.Payments;
using Payment.Application.Dtos;

namespace Payment.Application.Features.Payments.Commands.RetryPayment;

public sealed class RetryPaymentCommand : ICommand<PaymentDto>
{
    public Guid TripId { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public Guid IdempotencyKey { get; init; }
    public string Currency { get; init; } = "VND";
    public string? GatewayToken { get; init; }
}
