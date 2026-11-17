using FluentValidation;
using Trip.Domain.Enums;
namespace Trip.Application.Features.Trips.Queries.GetAllTrips;
public sealed class GetAllTripsQueryValidator : AbstractValidator<GetAllTripsQuery>
{
    public GetAllTripsQueryValidator()
    {
        RuleFor(x => x.RiderId).NotEqual(Guid.Empty).When(x => x.RiderId.HasValue);
        RuleFor(x => x.DriverId).NotEqual(Guid.Empty).When(x => x.DriverId.HasValue);
        RuleFor(x => x.Status).IsEnumName(typeof(TripStatus), false).When(x => !string.IsNullOrWhiteSpace(x.Status));
    }
}
