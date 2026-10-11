using System.Text.Json.Serialization;
using Payment.Application.Dtos;
using Shared.CQRS;
using Shared.Enums.Payments;

namespace Payment.Application.Features.Payments.Commands.CreatePayment;

public sealed record CreatePaymentCommand : ICommand<PaymentDto>
{
    public Guid TripId { get; init; }
    public Guid RiderId { get; init; }
    public Guid DriverId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public Guid IdempotencyKey { get; init; }
    public string Currency { get; init; } = "VND";
    public string? GatewayToken { get; init; }
    [JsonIgnore]
    public Guid CorrelationId { get; init; }
}
