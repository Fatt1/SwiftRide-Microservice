using FluentValidation;
namespace Trip.Application.Features.Trips.Queries.GetTripById;
public sealed class GetTripByIdQueryValidator : AbstractValidator<GetTripByIdQuery>
{
    public GetTripByIdQueryValidator() => RuleFor(x => x.Id).NotEmpty();
}
