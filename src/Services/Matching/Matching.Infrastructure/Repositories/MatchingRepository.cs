using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using MongoDB.Driver;

namespace Matching.Infrastructure.Repositories;

public class MatchingRepository : IMatchingRepository
{
    private readonly IMongoClient _mongoClient;

    public MatchingRepository(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }
    public Task AddDriverAttemptAsync(string sessionId, Guid driverId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task CreateAsync(MatchSession session, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<MatchSession>> GetAllMatchingSessionAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<MatchSession?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<MatchSession> GetByTripIdAsync(Guid tripId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(MatchSession session, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
