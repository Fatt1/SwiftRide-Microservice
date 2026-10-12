using Trip.Domain.Entities;

namespace Trip.Domain.Repositories;

public interface ITripRepository
{
    Task<Entities.Trip?> FindByIdAsync(Guid id);
    Task<Guid> CreateAsync(Entities.Trip trip);
    Task Update(Entities.Trip trip);
    Task<int> SaveChangesAsync();
}
