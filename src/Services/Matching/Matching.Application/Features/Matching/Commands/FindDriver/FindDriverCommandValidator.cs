using FluentValidation;
namespace Matching.Application.Features.Matching.Commands.FindDriver;
public sealed class FindDriverCommandValidator : AbstractValidator<FindDriverCommand>
{
    public FindDriverCommandValidator() => RuleFor(x => x.TripId).NotEmpty();
}
