using System.Security.Cryptography;

using UseCases.Abstractions;

namespace Infrastructure.Encryption;

public class KeyManagementService : IKeyManagementService
{
    public async Task<(string key, string iv)> GenerateKeyAsync()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.GenerateKey();
        aes.GenerateIV();
        await Task.Delay(0); // Simulate a slow key generation process

        return (Convert.ToBase64String(aes.Key), Convert.ToBase64String(aes.IV));
    }

    public string EncryptKey(string key, string masterKey)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(masterKey);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var keyBytes = Convert.FromBase64String(key);
        var encryptedBytes = encryptor.TransformFinalBlock(keyBytes, 0, keyBytes.Length);

        // Combine IV and encrypted key
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string DecryptKey(string encryptedKey, string masterKey)
    {
        var fullBytes = Convert.FromBase64String(encryptedKey);

        using var aes = Aes.Create();
        var iv = new byte[16];
        var cipher = new byte[fullBytes.Length - 16];

        Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(fullBytes, 16, cipher, 0, fullBytes.Length - 16);

        aes.Key = Convert.FromBase64String(masterKey);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Convert.ToBase64String(decryptedBytes);
    }
}