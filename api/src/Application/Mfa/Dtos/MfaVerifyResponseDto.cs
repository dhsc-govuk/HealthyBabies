namespace Application.Mfa.Dtos;

public record MfaVerifyResponseDto(
    bool Success,
    Guid SessionId,
    DateTime ExpiresAt);