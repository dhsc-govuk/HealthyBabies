namespace Application.Mfa.Dtos;

public enum MfaState
{
    None, // No MFA record - user has never set up MFA
    PendingSetup, // MFA record exists but user hasn't completed setup yet
    Enabled, // MFA is enabled and active
    Disabled // MFA was set up before but is currently disabled
}

public record MfaStatusDto(
    bool IsEnabled,
    DateTime? EnabledAt,
    int RecoveryCodesRemaining,
    MfaState State);