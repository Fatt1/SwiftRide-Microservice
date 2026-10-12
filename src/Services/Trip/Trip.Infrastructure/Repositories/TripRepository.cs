using Infrastructure.Common;
using Trip.Domain.Entities;
using Trip.Domain.Repositories;
using Trip.Infrastructure.Persistence;

namespace Trip.Infrastructure.Repositories;

public class TripRepository : RepositoryBase<Domain.Entities.Trip, Guid, TripDbContext>, ITripRepository
{
    public TripRepository(TripDbContext context) : base(context)
    {
    }
}
