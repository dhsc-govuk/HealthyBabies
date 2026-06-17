using Domain.Users;

namespace Application.Mfa.Exceptions;

public abstract class MfaException(UserId userId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public UserId UserId { get; } = userId;
}

public class MfaRequiredException(UserId userId)
    : MfaException(userId, $"MFA verification required for user {userId}");

public class MfaNotEnabledException(UserId userId)
    : MfaException(userId, $"MFA is not enabled for user {userId}");

public class MfaAlreadyEnabledException(UserId userId)
    : MfaException(userId, $"MFA is already enabled for user {userId}");

public class MfaNotSetupException(UserId userId)
    : MfaException(userId, $"MFA has not been set up for user {userId}. User must complete MFA setup first.");

public class InvalidMfaCodeException(UserId userId)
    : MfaException(userId, $"Invalid MFA code provided for user {userId}");

public class MfaRateLimitException(UserId userId, DateTime? lockedUntil = null)
    : MfaException(userId, $"Too many MFA attempts for user {userId}. Try again later.")
{
    public DateTime? LockedUntil { get; } = lockedUntil;
}

public class MfaSetupPendingException(UserId userId)
    : MfaException(userId, $"MFA setup is pending for user {userId}. Please complete setup.");

public class MfaSetupExpiredException(UserId userId)
    : MfaException(userId, $"MFA setup has expired for user {userId}. Please start again.");

public class InvalidRecoveryCodeException(UserId userId)
    : MfaException(userId, $"Invalid recovery code provided for user {userId}");

public class MfaUnknownException(UserId userId, Exception innerException)
    : MfaException(userId, $"Unknown MFA error for user {userId}", innerException);