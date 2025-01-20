namespace UseCases.Abstractions;

public interface IAesEncryptionService
{
    Task<string> Encrypt(string plainText, string key, string iv);
    Task<string> Decrypt(string cipherText, string key, string iv);
}