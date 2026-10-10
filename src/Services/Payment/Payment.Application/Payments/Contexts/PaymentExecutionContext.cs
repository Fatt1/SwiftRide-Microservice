using Payment.Domain.Entities;

namespace Payment.Application.Payments.Contexts;

public sealed class PaymentExecutionContext(PaymentTransaction payment)
{
    public PaymentTransaction Payment { get; } = payment;
    public Guid? DebitWalletId { get; set; }
    public Guid CreditWalletId { get; set; }
    public string? ProcessorResponse { get; set; }
}
