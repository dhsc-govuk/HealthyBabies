using Domain.Common;

namespace Domain.Users;

public class UserMfa : IEntity<UserMfaId>
{
    public UserMfaId Id { get; private set; }
    public UserId UserId { get; private set; }
    public string EncryptedSecret { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public List<string> HashedRecoveryCodes { get; private set; }
    public int FailedAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UserMfa(
        UserMfaId id,
        UserId userId,
        string encryptedSecret,
        bool isEnabled,
        DateTime? enabledAt,
        List<string> hashedRecoveryCodes,
        int failedAttempts,
        DateTime? lockedUntil,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        Id = id;
        UserId = userId;
        EncryptedSecret = encryptedSecret;
        IsEnabled = isEnabled;
        EnabledAt = enabledAt;
        HashedRecoveryCodes = hashedRecoveryCodes;
        FailedAttempts = failedAttempts;
        LockedUntil = lockedUntil;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static UserMfa New(UserMfaId id, UserId userId, string encryptedSecret)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNull(userId, nameof(userId));
        Guard.NotNullOrEmpty(encryptedSecret, nameof(encryptedSecret));

        return new UserMfa(
            id,
            userId,
            encryptedSecret,
            isEnabled: false,
            enabledAt: null,
            hashedRecoveryCodes: new List<string>(),
            failedAttempts: 0,
            lockedUntil: null,
            createdAt: DateTime.UtcNow,
            updatedAt: null);
    }

    public static UserMfa Create(UserId userId)
    {
        Guard.NotNull(userId, nameof(userId));

        return new UserMfa(
            UserMfaId.New(),
            userId,
            encryptedSecret: string.Empty,
            isEnabled: false,
            enabledAt: null,
            hashedRecoveryCodes: new List<string>(),
            failedAttempts: 0,
            lockedUntil: null,
            createdAt: DateTime.UtcNow,
            updatedAt: null);
    }

    public void Enable()
    {
        IsEnabled = true;
        EnabledAt = DateTime.UtcNow;
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable(List<string> hashedRecoveryCodes)
    {
        Guard.NotNull(hashedRecoveryCodes, nameof(hashedRecoveryCodes));

        IsEnabled = true;
        EnabledAt = DateTime.UtcNow;
        HashedRecoveryCodes = hashedRecoveryCodes;
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        EnabledAt = null;
        EncryptedSecret = string.Empty;
        HashedRecoveryCodes = new List<string>();
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedAttempt(int maxAttempts = 5, int lockoutMinutes = 30)
    {
        FailedAttempts++;
        if (FailedAttempts >= maxAttempts)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetFailedAttempts()
    {
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLockedOut() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    public void UpdateRecoveryCodes(List<string> hashedRecoveryCodes)
    {
        Guard.NotNull(hashedRecoveryCodes, nameof(hashedRecoveryCodes));
        HashedRecoveryCodes = hashedRecoveryCodes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveRecoveryCode(string hashedCode)
    {
        HashedRecoveryCodes.Remove(hashedCode);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSecret(string encryptedSecret)
    {
        Guard.NotNullOrEmpty(encryptedSecret, nameof(encryptedSecret));
        EncryptedSecret = encryptedSecret;
        UpdatedAt = DateTime.UtcNow;
    }
}