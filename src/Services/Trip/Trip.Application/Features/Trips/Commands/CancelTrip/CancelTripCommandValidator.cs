using FluentValidation;
namespace Trip.Application.Features.Trips.Commands.CancelTrip;
public sealed class CancelTripCommandValidator : AbstractValidator<CancelTripCommand>
{
    public CancelTripCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
    }
}
