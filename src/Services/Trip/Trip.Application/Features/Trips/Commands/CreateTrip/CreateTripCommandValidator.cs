using FluentValidation;
namespace Trip.Application.Features.Trips.Commands.CreateTrip;
public sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(x => x.RiderId).NotEmpty();
        RuleFor(x => x.PickupAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DropoffAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PickupLat).Must(double.IsFinite).InclusiveBetween(-90, 90);
        RuleFor(x => x.DropoffLat).Must(double.IsFinite).InclusiveBetween(-90, 90);
        RuleFor(x => x.PickupLng).Must(double.IsFinite).InclusiveBetween(-180, 180);
        RuleFor(x => x.DropoffLng).Must(double.IsFinite).InclusiveBetween(-180, 180);
    }
}
