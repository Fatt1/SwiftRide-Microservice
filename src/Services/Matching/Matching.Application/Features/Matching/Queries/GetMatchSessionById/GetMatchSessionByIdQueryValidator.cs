using FluentValidation;
using MongoDB.Bson;
namespace Matching.Application.Features.Matching.Queries.GetMatchSessionById;
public sealed class GetMatchSessionByIdQueryValidator : AbstractValidator<GetMatchSessionByIdQuery>
{
    public GetMatchSessionByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("A valid MongoDB session ID is required.");
    }
}
