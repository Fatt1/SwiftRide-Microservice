using FluentValidation;
using MediatR;
using MassTransit;
using Shared;
using Shared.CQRS.Behaviors;
using Trip.Domain.Repositories;
using Trip.Application.Features.Trips.Commands.CancelTrip;
using Trip.Application.Features.Trips.Commands.PickUp;
using Trip.Application.Features.Trips.Commands.DropOff;
using Trip.Application.Features.Trips.Commands.DriverAccept;
using Trip.Application.Features.Trips.Commands.DriverReject;
using Trip.Application.DTOs;
using Trip.Application.Features.Trips.Queries.GetTripById;
namespace Trip.UnitTests;
internal static class ValidatedHandlers
{
    public static IRequestHandler<CancelTripCommand, Result> CancelTrip(ITripRepository repository, IPublishEndpoint publisher)
    {
        return Wrap(new CancelTripHandler(repository, publisher), new CancelTripCommandValidator());
    }
    public static IRequestHandler<PickUpCommand, Result> PickUp(ITripRepository repository)
    {
        return Wrap(new PickUpHandler(repository), new PickUpCommandValidator());
    }
    public static IRequestHandler<DropOffCommand, Result> DropOff(ITripRepository repository, IPublishEndpoint publisher)
    {
        return Wrap(new DropOffHandler(repository, publisher), new DropOffCommandValidator());
    }
    public static IRequestHandler<DriverAcceptCommand, Result> DriverAccept(ITripRepository repository, IPublishEndpoint publisher)
    {
        return Wrap(new DriverAcceptHandler(repository, publisher), new DriverAcceptCommandValidator());
    }
    public static IRequestHandler<DriverRejectCommand, Result<DriverRejectResponse>> DriverReject(ITripRepository repository, IPublishEndpoint publisher)
    {
        return Wrap(new DriverRejectHandler(repository, publisher), new DriverRejectCommandValidator());
    }
    public static IRequestHandler<GetTripByIdQuery, Result<TripDto>> GetTripById(ITripRepository repository)
    {
        return Wrap(new GetTripByIdHandler(repository), new GetTripByIdQueryValidator());
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
