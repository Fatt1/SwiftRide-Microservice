using FluentValidation;
using MediatR;
using MassTransit;
using Shared;
using Shared.CQRS.Behaviors;
using Matching.Application.Abstractions;
using Matching.Application.Features.Matching.Commands.FindDriver;
using Matching.Application.Features.Matching.Queries.GetMatchSessionById;
using Matching.Application.Features.Matching.Dtos;
using Matching.Domain.Entities;
using Matching.Domain.Repositories;
namespace Matching.UnitTests;
internal static class ValidatedHandlers
{
    public static IRequestHandler<FindDriverCommand, Result<FindDriverResult>> FindDriver(IMatchingRepository sessions,
        IDriverLocationRepository drivers, IPublishEndpoint publisher)
    {
        return Wrap(new FindDriverCommandHandler(sessions, drivers, publisher), new FindDriverCommandValidator());
    }
    public static IRequestHandler<GetMatchSessionByIdQuery, Result<MatchSessionDto>> GetMatchSessionById(IMatchingRepository sessions)
    {
        return Wrap(new GetMatchSessionByIdHandler(sessions), new GetMatchSessionByIdQueryValidator());
    }

    private static IRequestHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler, IValidator<TRequest> validator)
        where TRequest : notnull, IRequest<TResponse> where TResponse : Result => new PipelineHandler<TRequest, TResponse>(handler, validator);
    private sealed class PipelineHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler, IValidator<TRequest> validator)
        : IRequestHandler<TRequest, TResponse> where TRequest : notnull, IRequest<TResponse> where TResponse : Result
    {
        public Task<TResponse> Handle(TRequest request, CancellationToken ct) =>
            new ValidationBehavior<TRequest, TResponse>([validator]).Handle(request, token => handler.Handle(request, token), ct);
    }
}
