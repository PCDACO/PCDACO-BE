using Domain.Entities;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.Abstractions;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateEncryptionKey
{
    private static readonly IKeyManagementService _keyService = new KeyManagementService();

    private static async Task<EncryptionKey> CreateEncryptionKey()
    {
        var (key, iv) = await _keyService.GenerateKeyAsync();

        return new EncryptionKey
        {
            Id = Guid.NewGuid(),
            EncryptedKey = _keyService.EncryptKey(key, TestConstants.MasterKey),
            IV = iv,
        };
    }

    public static async Task<EncryptionKey> CreateTestEncryptionKey(AppDBContext dBContext)
    {
        var encryptionKey = await CreateEncryptionKey();
        await dBContext.EncryptionKeys.AddAsync(encryptionKey);
        await dBContext.SaveChangesAsync();

        return encryptionKey;
    }
}
