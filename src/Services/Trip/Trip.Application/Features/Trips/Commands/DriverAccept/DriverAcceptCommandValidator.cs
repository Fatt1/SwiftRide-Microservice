using FluentValidation;
namespace Trip.Application.Features.Trips.Commands.DriverAccept;
public sealed class DriverAcceptCommandValidator : AbstractValidator<DriverAcceptCommand>
{
    public DriverAcceptCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
    }
}
