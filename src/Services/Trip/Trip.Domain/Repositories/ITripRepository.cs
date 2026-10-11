using Trip.Domain.Entities;

namespace Trip.Domain.Repositories;

public interface ITripRepository
{
    Task<Trip?> FindByIdAsync(Guid id);
    Task<Guid> CreateAsync(Trip trip);
    Task Update(Trip trip);
    Task<int> SaveChangesAsync();
}
