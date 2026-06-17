using Domain.Common;

namespace Domain.Users;

public class MfaSession : IEntity<MfaSessionId>
{
    public MfaSessionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public string IpAddressHash { get; private set; }
    public string UserAgentHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MfaSession(
        MfaSessionId id,
        UserId userId,
        string ipAddressHash,
        string userAgentHash,
        DateTime expiresAt,
        bool isRevoked,
        DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        IpAddressHash = ipAddressHash;
        UserAgentHash = userAgentHash;
        ExpiresAt = expiresAt;
        IsRevoked = isRevoked;
        CreatedAt = createdAt;
    }

    public static MfaSession New(
        MfaSessionId id,
        UserId userId,
        string ipAddressHash,
        string userAgentHash,
        int expiryHours = 8)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNull(userId, nameof(userId));
        Guard.NotNullOrEmpty(ipAddressHash, nameof(ipAddressHash));
        Guard.NotNullOrEmpty(userAgentHash, nameof(userAgentHash));

        return new MfaSession(
            id,
            userId,
            ipAddressHash,
            userAgentHash,
            expiresAt: DateTime.UtcNow.AddHours(expiryHours),
            isRevoked: false,
            createdAt: DateTime.UtcNow);
    }

    // BLUM-6354: Intentionally does not compare IpAddressHash. Traffic alternates between
    // Azure Front Door (sets X-Azure-ClientIP) and an internal load balancer (sets only
    // X-Forwarded-For), so the resolved client IP flaps between requests in the same
    // session and the hash mismatches. Remaining controls protect the session:
    //   - JWT (Entra) is still required on every request and UserId is matched against
    //     the session in MfaRequiredMiddleware (cookie alone is useless).
    //   - mfa_session cookie is HttpOnly + Secure + cryptographic GUID.
    //   - UserAgent hash, expiry, and explicit revocation are still enforced here.
    // IpAddressHash is still stored at creation time for audit/forensics.
    public bool IsValid(string currentUserAgentHash)
    {
        if (IsRevoked)
        {
            return false;
        }

        if (DateTime.UtcNow > ExpiresAt)
        {
            return false;
        }

        if (UserAgentHash != currentUserAgentHash)
        {
            return false;
        }

        return true;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public void Revoke()
    {
        IsRevoked = true;
    }
}