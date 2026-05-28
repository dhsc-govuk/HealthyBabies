namespace Application.Common.Interfaces;

public interface ITotpService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string email, string secret, string issuer = "FamilyHubs");
    bool ValidateCode(string secret, string code);
}