using Application.Common.Interfaces.Repositories;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class MfaSessionRepository(ApplicationDbContext dbContext) : IMfaSessionRepository
{
    public async Task<MfaSession?> GetByIdAsync(MfaSessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.MfaSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<List<MfaSession>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.MfaSessions
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync(cancellationToken);
    }

    public async Task<MfaSession> AddAsync(MfaSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.MfaSessions.AddAsync(session, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<MfaSession> UpdateAsync(MfaSession session, CancellationToken cancellationToken = default)
    {
        dbContext.MfaSessions.Update(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSessions = await dbContext.MfaSessions
            .Where(x => x.ExpiresAt < DateTime.UtcNow || x.IsRevoked)
            .ToListAsync(cancellationToken);

        dbContext.MfaSessions.RemoveRange(expiredSessions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}