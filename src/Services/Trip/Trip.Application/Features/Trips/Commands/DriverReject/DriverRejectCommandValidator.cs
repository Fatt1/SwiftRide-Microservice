using FluentValidation;
namespace Trip.Application.Features.Trips.Commands.DriverReject;
public sealed class DriverRejectCommandValidator : AbstractValidator<DriverRejectCommand>
{
    public DriverRejectCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
    }
}
