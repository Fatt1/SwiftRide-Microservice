using FluentValidation;
using Shared.Enums.Payments;

namespace Payment.Application.Features.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.RiderId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty().NotEqual(x => x.RiderId);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Currency).Equal("VND");
        RuleFor(x => x.GatewayToken).NotEmpty().MaximumLength(128)
            .When(x => x.PaymentMethod == PaymentMethod.Card);
    }
}
