using FluentValidation;
using Shared.Enums.Payments;

namespace Payment.Application.Payments;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.RiderId).NotEmpty().NotEqual(x => x.DriverId);
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.CorrelationId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).Must(x => decimal.Truncate(x) == x);
        RuleFor(x => x.Currency).Equal("VND");
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.GatewayToken).NotEmpty().When(x => x.PaymentMethod == PaymentMethod.Card);
        RuleFor(x => x.GatewayToken).Null().When(x => x.PaymentMethod == PaymentMethod.Wallet);
    }
}

public sealed class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
