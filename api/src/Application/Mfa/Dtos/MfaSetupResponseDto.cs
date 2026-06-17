namespace Application.Mfa.Dtos;

public record MfaSetupResponseDto(
    string QrCodeUri,
    string ManualEntryKey);