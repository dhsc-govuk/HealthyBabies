namespace Application.Common.Interfaces;

public interface IRecoveryCodeService
{
    List<string> GenerateRecoveryCodes(int count = 10);
    string HashRecoveryCode(string code);
    bool VerifyRecoveryCode(string code, string hashedCode);
}