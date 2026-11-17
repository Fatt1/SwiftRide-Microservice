using FluentValidation;
namespace Trip.Application.Features.Trips.Commands.DropOff;
public sealed class DropOffCommandValidator : AbstractValidator<DropOffCommand>
{
    public DropOffCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.PaymentMethod).IsInEnum().WithMessage("Payment method is invalid.");
        RuleFor(x => x.GatewayToken).NotEmpty().WithMessage("GatewayToken is required for card payments (maximum 128 characters).")
            .MaximumLength(128).WithMessage("GatewayToken is required for card payments (maximum 128 characters).")
            .When(x => x.PaymentMethod == Shared.Enums.Payments.PaymentMethod.Card);
    }
}
