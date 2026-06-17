using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Common.Settings;
using Domain.Users;

namespace Infrastructure.Mfa;

public class MfaSessionService(IMfaSessionRepository sessionRepository, ApplicationSettings settings) : IMfaSessionService
{
    public string HashIpAddress(string ipAddress)
    {
        return ComputeHash(ipAddress);
    }

    public string HashUserAgent(string userAgent)
    {
        return ComputeHash(userAgent);
    }

    private static string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    public async Task<MfaSession> CreateSessionAsync(
        UserId userId,
        string ipAddress,
        string userAgent)
    {
        var session = MfaSession.New(
            MfaSessionId.New(),
            userId,
            HashIpAddress(ipAddress),
            HashUserAgent(userAgent),
            settings.Mfa.SessionExpiryHours);

        await sessionRepository.AddAsync(session);
        return session;
    }

    public async Task<MfaSession?> ValidateSessionAsync(
        MfaSessionId sessionId,
        string ipAddress,
        string userAgent)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return null;
        }

        // ipAddress retained on the signature for caller stability; not compared (see BLUM-6354 / MfaSession.IsValid).
        var currentUserAgentHash = HashUserAgent(userAgent);

        if (!session.IsValid(currentUserAgentHash))
        {
            return null;
        }

        return session;
    }

    public async Task RevokeSessionAsync(MfaSessionId sessionId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session != null)
        {
            session.Revoke();
            await sessionRepository.UpdateAsync(session);
        }
    }

    public async Task RevokeAllUserSessionsAsync(UserId userId)
    {
        var sessions = await sessionRepository.GetByUserIdAsync(userId);
        foreach (var session in sessions)
        {
            session.Revoke();
            await sessionRepository.UpdateAsync(session);
        }
    }
}