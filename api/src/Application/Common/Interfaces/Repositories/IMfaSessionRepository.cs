using Domain.Users;

namespace Application.Common.Interfaces.Repositories;

public interface IMfaSessionRepository
{
    Task<MfaSession?> GetByIdAsync(MfaSessionId sessionId, CancellationToken cancellationToken = default);
    Task<List<MfaSession>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<MfaSession> AddAsync(MfaSession session, CancellationToken cancellationToken = default);
    Task<MfaSession> UpdateAsync(MfaSession session, CancellationToken cancellationToken = default);
    Task DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default);
}