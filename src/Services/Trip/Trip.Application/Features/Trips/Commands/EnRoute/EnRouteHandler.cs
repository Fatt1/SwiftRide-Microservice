using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
namespace Trip.Application.Features.Trips.Commands.EnRoute;
public sealed record EnRouteCommand(Guid TripId) : ICommand;

internal sealed class EnRouteHandler(ITripRepository trips) : ICommandHandler<EnRouteCommand>
{
    public async Task<Result> Handle(EnRouteCommand request, CancellationToken ct)
    {
        var trip = await trips.FindByIdAsync(request.TripId);
        if (trip is null) 
        return Result.Failure(new NotFoundError("Trip", request.TripId));

        if (trip.Status != TripStatus.DriverAccepted) 
        return Result.Failure(new ConflictError("Driver must accept first."));
        trip.MarkEnRoute(); 
        await trips.Update(trip);
        await trips.SaveChangesAsync(); 
        return Result.Success();
    }
}
