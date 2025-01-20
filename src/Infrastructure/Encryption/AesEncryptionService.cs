using System.Security.Cryptography;

using UseCases.Abstractions;

namespace Infrastructure.Encryption;

public class AesEncryptionService : IAesEncryptionService
{
    public async Task<string> Encrypt(string plainText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256; // Explicitly set key size
        aes.BlockSize = 128; // AES block size is always 128 bits
        aes.Key = Convert.FromBase64String(key);
        aes.IV = Convert.FromBase64String(iv);

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            await swEncrypt.WriteAsync(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public async Task<string> Decrypt(string cipherText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Key = Convert.FromBase64String(key);
        aes.IV = Convert.FromBase64String(iv);

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return await srDecrypt.ReadToEndAsync();
    }
}