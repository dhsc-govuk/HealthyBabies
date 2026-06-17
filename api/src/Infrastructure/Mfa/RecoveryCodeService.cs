using System.Security.Cryptography;
using Application.Common.Interfaces;

namespace Infrastructure.Mfa;

public class RecoveryCodeService : IRecoveryCodeService
{
    private const int CodeLength = 12;
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            codes.Add(GenerateCode());
        }

        return codes;
    }

    private static string GenerateCode()
    {
        var code = new char[CodeLength];
        var randomBytes = new byte[CodeLength];
        RandomNumberGenerator.Fill(randomBytes);

        for (var i = 0; i < CodeLength; i++)
        {
            code[i] = AllowedChars[randomBytes[i] % AllowedChars.Length];
        }

        return $"{new string(code[..4])}-{new string(code[4..8])}-{new string(code[8..12])}";
    }

    public string HashRecoveryCode(string code)
    {
        var normalizedCode = code.Replace("-", string.Empty).ToUpperInvariant();
        return BCrypt.Net.BCrypt.HashPassword(normalizedCode, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyRecoveryCode(string code, string hashedCode)
    {
        var normalizedCode = code.Replace("-", string.Empty).ToUpperInvariant();
        return BCrypt.Net.BCrypt.Verify(normalizedCode, hashedCode);
    }
}