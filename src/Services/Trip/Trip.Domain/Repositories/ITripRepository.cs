using Contracts.Common.Interfaces;
using Trip.Domain.Entities;

namespace Trip.Domain.Repositories;

public interface ITripRepository : IRepositoryBase<Entities.Trip, Guid>
{
}
