namespace Application.Mfa.Dtos;

public record MfaSetupCompleteResponseDto(
    bool Success,
    List<string> RecoveryCodes,
    Guid SessionId,
    DateTime SessionExpiresAt);