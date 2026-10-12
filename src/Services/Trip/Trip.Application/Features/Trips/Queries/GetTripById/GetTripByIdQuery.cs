using Shared.CQRS;
using Trip.Application.DTOs;

namespace Trip.Application.Features.Trips.Queries.GetTripById;

public sealed record GetTripByIdQuery(Guid Id) : IQuery<TripDto>;
