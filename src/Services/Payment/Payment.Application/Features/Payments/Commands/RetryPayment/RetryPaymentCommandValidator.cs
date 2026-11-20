using FluentValidation;
using Shared.Enums.Payments;

namespace Payment.Application.Features.Payments.Commands.RetryPayment;

public sealed class RetryPaymentCommandValidator : AbstractValidator<RetryPaymentCommand>
{
    public RetryPaymentCommandValidator()
    {
        RuleFor(request => request.TripId).NotEmpty();
        RuleFor(request => request.PaymentMethod).IsInEnum();
        RuleFor(request => request.IdempotencyKey).NotEmpty();
        RuleFor(request => request.Currency).Equal("VND");
        RuleFor(request => request.GatewayToken).NotEmpty().MaximumLength(128)
            .When(request => request.PaymentMethod == PaymentMethod.Card);
    }
}
