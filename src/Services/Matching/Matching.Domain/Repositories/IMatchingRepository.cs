using Matching.Domain.Entities;

namespace Matching.Domain.Repositories;

public interface IMatchingRepository
{

    Task<MatchSession?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<MatchSession>> GetAllMatchingSessionAsync(CancellationToken ct = default);
    Task CreateAsync(MatchSession session, CancellationToken ct = default);
    Task AddDriverAttemptAsync(string sessionId, Guid driverId, CancellationToken ct = default);

    Task UpdateAsync(MatchSession session, CancellationToken ct = default);

    Task<MatchSession?> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);
}
