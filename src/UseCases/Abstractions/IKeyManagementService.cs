namespace UseCases.Abstractions;

public interface IKeyManagementService
{
    Task<(string key, string iv)> GenerateKeyAsync();
    string EncryptKey(string key, string masterKey);
    string DecryptKey(string encryptedKey, string masterKey);
}