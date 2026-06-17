using Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.Mfa;

public class MfaEncryptionService : IMfaEncryptionService
{
    private readonly IDataProtector _protector;
    private const string Purpose = "MfaSecretProtection";

    public MfaEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            throw new ArgumentNullException(nameof(cipherText));
        }

        return _protector.Unprotect(cipherText);
    }
}