using Payment.Application.Payments.Contexts;
using Shared;
using Shared.Enums.Payments;

namespace Payment.Application.Abstractions;

public interface IPaymentStrategy
{
    PaymentMethod Method { get; }
    Task<Result> ChargeAsync(PaymentExecutionContext context, CancellationToken ct);
    Task<Result> RefundAsync(RefundExecutionContext context, CancellationToken ct);
}
