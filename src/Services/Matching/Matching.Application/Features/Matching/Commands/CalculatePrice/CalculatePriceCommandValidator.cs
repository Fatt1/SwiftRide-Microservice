using FluentValidation;
namespace Matching.Application.Features.Matching.Commands.CalculatePrice;
public sealed class CalculatePriceCommandValidator : AbstractValidator<CalculatePriceCommand>
{
    public CalculatePriceCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.PickupLatitude).Must(double.IsFinite).InclusiveBetween(-90, 90);
        RuleFor(x => x.DestinationLatitude).Must(double.IsFinite).InclusiveBetween(-90, 90);
        RuleFor(x => x.PickupLongitude).Must(double.IsFinite).InclusiveBetween(-180, 180);
        RuleFor(x => x.DestinationLongitude).Must(double.IsFinite).InclusiveBetween(-180, 180);
    }
}
