using FluentValidation;
namespace Trip.Application.Features.Trips.Commands.PickUp;
public sealed class PickUpCommandValidator : AbstractValidator<PickUpCommand>
{
    public PickUpCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
    }
}
