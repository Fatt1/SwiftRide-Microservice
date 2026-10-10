using System.Text.Json.Serialization;
using Payment.Application.Dtos;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Commands.RefundPayment;

public sealed record RefundPaymentCommand : ICommand<RefundDto>
{
    [JsonIgnore]
    public Guid PaymentId { get; init; }
    public string Reason { get; init; } = "";
}
