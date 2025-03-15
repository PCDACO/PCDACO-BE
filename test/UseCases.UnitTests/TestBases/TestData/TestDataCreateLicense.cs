using Domain.Entities;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateLicense
{
    private static async Task<User?> CreateLicenseAsync(
        AppDBContext dbContext,
        Guid userId,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyService,
        EncryptionSettings encryptionSettings,
        string licenseNumber = "123456789",
        bool? isApproved = null
    )
    {
        (string key, string iv) = await keyService.GenerateKeyAsync();
        string encryptedKey = keyService.EncryptKey(key, encryptionSettings.Key);
        string encryptedLicenseNumber = await aesEncryptionService.Encrypt(licenseNumber, key, iv);

        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await dbContext.SaveChangesAsync();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null)
            return null;

        user.EncryptionKeyId = encryptionKey.Id;
        user.EncryptedLicenseNumber = encryptedLicenseNumber;
        user.LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1);
        user.LicenseImageFrontUrl = "front-url";
        user.LicenseImageBackUrl = "back-url";
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.LicenseIsApproved = isApproved;
        user.LicenseImageUploadedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();

        return user;
    }

    public static async Task<User> CreateTestLicense(
        AppDBContext dBContext,
        Guid userId,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        bool? isApproved = null
    )
    {
        var license = await CreateLicenseAsync(
            dbContext: dBContext,
            userId: userId,
            aesEncryptionService: aesEncryptionService,
            keyService: keyManagementService,
            encryptionSettings: encryptionSettings,
            isApproved: isApproved
        );

        return license!;
    }

    public static async Task<User> CreateTestLicense(
        AppDBContext dBContext,
        Guid userId,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        string licenseNumber
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var license = await CreateLicenseAsync(
            dbContext: dBContext,
            userId: userId,
            aesEncryptionService: aesEncryptionService,
            keyService: keyManagementService,
            encryptionSettings: encryptionSettings,
            licenseNumber: licenseNumber
        );

        return license!;
    }
}
