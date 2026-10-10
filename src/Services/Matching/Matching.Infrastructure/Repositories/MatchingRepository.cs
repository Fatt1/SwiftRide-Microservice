using Matching.Domain.Entities;
using Matching.Domain.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Matching.Infrastructure.Repositories;

public class MatchingRepository : IMatchingRepository
{
    private readonly IMongoCollection<MatchSession> _sessions;

    public MatchingRepository(IMongoDatabase database)
    {
        _sessions = database.GetCollection<MatchSession>("match_sessions");
    }
    public async Task AddDriverAttemptAsync(string sessionId, Guid driverId, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(sessionId, out _))
            throw new ArgumentException("Invalid match session id.", nameof(sessionId));



        var now = DateTime.UtcNow;
        var filter = Builders<MatchSession>.Filter.Eq(x => x.Id, sessionId);
        var update = Builders<MatchSession>.Update
            .Push(x => x.DriverAttempts, new DriverAttempt
            {
                DriverId = driverId,
                AttemptedAt = now
            })
            .Set(x => x.LastModifiedAt, now);

        var result = await _sessions.UpdateOneAsync(
            filter,
            update,
            cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException(
                $"Match session '{sessionId}' was not found.");
    }

    public async Task CreateAsync(MatchSession session, CancellationToken ct = default)
    {
        await _sessions.InsertOneAsync(session, cancellationToken: ct);
    }

    public async Task<List<MatchSession>> GetAllMatchingSessionAsync(CancellationToken ct = default)
    {
        return await _sessions
            .Find(Builders<MatchSession>.Filter.Empty)
            .ToListAsync(ct);
    }

    public async Task<MatchSession?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        // Id của MatchSession được lưu trong MongoDB dưới dạng ObjectId.
        if (!ObjectId.TryParse(id, out _))
            return null;

        return await _sessions
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<MatchSession?> GetByTripIdAsync(Guid tripId, CancellationToken ct = default)
    {
        var session = await _sessions
            .Find(x => x.TripId == tripId)
            .FirstOrDefaultAsync(ct);

        return session;
    }

    public async Task UpdateAsync(MatchSession session, CancellationToken ct = default)
    {

        if (!ObjectId.TryParse(session.Id, out _))
            throw new ArgumentException("Invalid match session id.", nameof(session));

        await _sessions.ReplaceOneAsync(
           x => x.Id == session.Id,
           session,
           cancellationToken: ct);

    }
}
