using Domain.Entities;
using Domain.Shared;
using Persistance.Data;
using UseCases.Abstractions;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateLicense
{
    private static async Task<License> CreateLicenseAsync(
        AppDBContext dbContext,
        Guid userId,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyService,
        EncryptionSettings encryptionSettings,
        string licenseNumber = "123456789",
        bool? isApproved = null,
        bool isDeleted = false
    )
    {
        (string key, string iv) = await keyService.GenerateKeyAsync();
        string encryptedKey = keyService.EncryptKey(key, encryptionSettings.Key);
        string encryptedLicenseNumber = await aesEncryptionService.Encrypt(licenseNumber, key, iv);

        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await dbContext.SaveChangesAsync();

        return new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicenseNumber = encryptedLicenseNumber,
            ExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            LicenseImageFrontUrl = "front-url",
            LicenseImageBackUrl = "back-url",
            IsApprove = isApproved,
            IsDeleted = isDeleted,
        };
    }

    public static async Task<License> CreateTestLicense(
        AppDBContext dBContext,
        Guid userId,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        bool? isApproved = null,
        bool isDeleted = false
    )
    {
        var license = await CreateLicenseAsync(
            dbContext: dBContext,
            userId: userId,
            aesEncryptionService: aesEncryptionService,
            keyService: keyManagementService,
            encryptionSettings: encryptionSettings,
            isApproved: isApproved,
            isDeleted: isDeleted
        );

        await dBContext.Licenses.AddAsync(license);
        await dBContext.SaveChangesAsync();

        return license;
    }

    public static async Task<License> CreateTestLicense(
        AppDBContext dBContext,
        Guid userId,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        string licenseNumber,
        bool isDeleted = false
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var license = await CreateLicenseAsync(
            dbContext: dBContext,
            userId: userId,
            aesEncryptionService: aesEncryptionService,
            keyService: keyManagementService,
            encryptionSettings: encryptionSettings,
            licenseNumber: licenseNumber,
            isDeleted: isDeleted
        );

        await dBContext.Licenses.AddAsync(license);
        await dBContext.SaveChangesAsync();

        return license;
    }
}
