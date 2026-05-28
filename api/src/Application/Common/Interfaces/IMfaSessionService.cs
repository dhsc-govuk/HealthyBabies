using Domain.Users;

namespace Application.Common.Interfaces;

public interface IMfaSessionService
{
    string HashIpAddress(string ipAddress);
    string HashUserAgent(string userAgent);
    Task<MfaSession> CreateSessionAsync(UserId userId, string ipAddress, string userAgent);
    Task<MfaSession?> ValidateSessionAsync(MfaSessionId sessionId, string ipAddress, string userAgent);
    Task RevokeSessionAsync(MfaSessionId sessionId);
    Task RevokeAllUserSessionsAsync(UserId userId);
}