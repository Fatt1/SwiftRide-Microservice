using Payment.Domain.Entities;

namespace Payment.Application.Payments.Contexts;

public sealed class RefundExecutionContext(PaymentTransaction payment)
{
    public PaymentTransaction Payment { get; } = payment;
}
