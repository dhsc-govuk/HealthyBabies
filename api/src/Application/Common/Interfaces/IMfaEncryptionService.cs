namespace Application.Common.Interfaces;

public interface IMfaEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}