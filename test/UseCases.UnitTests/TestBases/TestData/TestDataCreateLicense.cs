using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateLicense
{
    private static License CreateLicense(
        Guid userId,
        Guid encryptionKeyId,
        string licenseNumber = "123456789",
        bool isDeleted = false
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            EncryptionKeyId = encryptionKeyId,
            EncryptedLicenseNumber = licenseNumber,
            ExpiryDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"),
            LicenseImageFrontUrl = "front-url",
            LicenseImageBackUrl = "back-url",
            IsDeleted = isDeleted,
        };

    public static async Task<License> CreateTestLicense(
        AppDBContext dBContext,
        Guid userId,
        bool isDeleted = false
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var license = CreateLicense(userId, encryptionKey.Id, isDeleted: isDeleted);

        await dBContext.Licenses.AddAsync(license);
        await dBContext.SaveChangesAsync();

        return license;
    }

    public static async Task<License> CreateTestLicense(
        AppDBContext dBContext,
        Guid userId,
        string licenseNumber,
        bool isDeleted = false
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var license = CreateLicense(userId, encryptionKey.Id, licenseNumber, isDeleted);

        await dBContext.Licenses.AddAsync(license);
        await dBContext.SaveChangesAsync();

        return license;
    }
}
