using Domain.Entities;
using Domain.Shared;
using UseCases.Abstractions;
using UseCases.Utils;
using UUIDNext;
using Database = UUIDNext.Database;

namespace Persistance.Bogus;

public class UserDummyData
{
    public Guid Id { get; set; } = Uuid.NewDatabaseFriendly(Database.PostgreSql);
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Phone { get; set; }
    public required string Address { get; set; }
    public DateTimeOffset DOB { get; set; } = DateTimeOffset.UtcNow;
    public required Guid RoleId { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseImageFrontUrl { get; set; }
    public string? LicenseImageBackUrl { get; set; }
    public DateTimeOffset? LicenseExpiryDate { get; set; }
    public DateTimeOffset? LicenseImageUploadedAt { get; set; }
}

public class UserGenerator
{
    public static UserDummyData[] dummyUsers =
    [
        // ADMIN
        new()
        {
            Id = Guid.Parse("01951ead-d228-7d1d-9174-d9e84d69c119"),
            Name = "ADMIN",
            Email = "admin@gmail.com",
            Password = "admin".HashString(),
            Phone = "0931512321",
            Address = "480/59A, Đường Bình Quới, Phường 28, Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e22-c88e-7c99-901e-23ff1ebccf85"),
        },
        // DRIVER
        new()
        {
            Id = Guid.Parse("01950d41-d234-7b63-a360-72b27605b4a4"),
            Name = "DRIVER",
            Email = "thinhdpham2510@gmail.com",
            Password = "Ph@mDucThinh25102003".HashString(),
            Phone = "0938396953",
            Address = "480/59A, Đường Bình Quới, Phường 28, Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e20-ab3f-722f-aceb-3485c166e8cf"),
        },
        new()
        {
            Id = Guid.Parse("01957a2a-27ca-7315-990e-71e56af48bfa"),
            Name = "Trung Anh Driver",
            Email = "anhthtse151299@gmail.com",
            Password = "@Trunganh123".HashString(),
            Phone = "0918516221",
            Address = "480/59A, Đường Bình Quới, Phường 28, Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e20-ab3f-722f-aceb-3485c166e8cf"),
        },
        // Owner
        new()
        {
            Id = Guid.Parse("01951eae-12a7-756d-a8d5-bb1ee525d7b5"),
            Name = "Owner",
            Email = "thinhmusicion@gmail.com",
            Password = "Ph@mDucThinh25102003".HashString(),
            Phone = "0877344076",
            Address = "312, Đường Xô Viết Nghệ Tĩnh, Phường 25, Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e20-7a6e-7106-a6f3-148b63f52149"),
        },
        new()
        {
            Id = Guid.Parse("01957a2a-4e2e-73a3-ac6a-1dfa84e48520"),
            Name = "Trung Anh Owner",
            Email = "anhthtservice@gmail.com",
            Password = "@Trunganh123".HashString(),
            Phone = "0961790276",
            Address = "312, Đường Xô Viết Nghệ Tĩnh, Phường 25, Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e20-7a6e-7106-a6f3-148b63f52149"),
        },
        // Technician
        new()
        {
            Id = Guid.Parse("01951eae-453b-7ad9-949f-63dd30b592e1"),
            Name = "Technician",
            Email = "technician@gmail.com",
            Password = "technician".HashString(),
            Phone = "0933221132",
            Address = "480/59A Đường Bình Quới Phường 28 Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e22-ee2e-7bbf-914e-e39f14e0f420"),
        },
        // Consultant
        new()
        {
            Id = Guid.Parse("01951eae-7342-78fb-ae8c-02ce503ed400"),
            Name = "Consultant",
            Email = "consultant@gmail.com",
            Password = "consultant".HashString(),
            Phone = "0918231512",
            Address = "480/59A Đường Bình Quới Phường 28 Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e22-dd78-7933-b742-76110d88728c"),
        },
        // Pending License Approval - Drivers
        new()
        {
            Id = Guid.Parse("01960d1a-b2e3-7d1c-a384-83b27605c5a5"),
            Name = "Driver Pending Approval 1",
            Email = "driver.pending1@example.com",
            Password = "Password123!".HashString(),
            Phone = "0901234567",
            Address = "123 Đường Nguyễn Văn Linh, Quận 7, TP.HCM",
            RoleId = Guid.Parse("01951e20-ab3f-722f-aceb-3485c166e8cf"), // Driver
            LicenseNumber = "79000123456",
            LicenseImageFrontUrl =
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRlOD0DOcgJ9c2cgjGZi5sb-4VZK8bdJLYiTQ&s",
            LicenseImageBackUrl =
                "https://inkythuatso.com/uploads/thumbnails/800/2022/08/hinh-anh-bang-lai-xe-b2-mat-sau-inkythuatso-09-15-26-07.jpg",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(5),
        },
        new()
        {
            Id = Guid.Parse("01960d1a-c4f9-7d2d-b492-94c38716d6b6"),
            Name = "Driver Pending Approval 2",
            Email = "driver.pending2@example.com",
            Password = "Password123!".HashString(),
            Phone = "0907654321",
            Address = "456 Đường Lê Văn Lương, Quận 7, TP.HCM",
            RoleId = Guid.Parse("01951e20-ab3f-722f-aceb-3485c166e8cf"), // Driver
            LicenseNumber = "79000654321",
            LicenseImageFrontUrl =
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRlOD0DOcgJ9c2cgjGZi5sb-4VZK8bdJLYiTQ&s",
            LicenseImageBackUrl =
                "https://inkythuatso.com/uploads/thumbnails/800/2022/08/hinh-anh-bang-lai-xe-b2-mat-sau-inkythuatso-09-15-26-07.jpg",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(3),
        },
        new()
        {
            Id = Guid.Parse("01960d1a-d5e8-7d39-c5a0-a5d497827dc7"),
            Name = "Driver Pending Approval 3",
            Email = "driver.pending3@example.com",
            Password = "Password123!".HashString(),
            Phone = "0912345678",
            Address = "789 Đường Phạm Văn Đồng, Quận Thủ Đức, TP.HCM",
            RoleId = Guid.Parse("01951e20-ab3f-722f-aceb-3485c166e8cf"), // Driver
            LicenseNumber = "79000789012",
            LicenseImageFrontUrl =
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRlOD0DOcgJ9c2cgjGZi5sb-4VZK8bdJLYiTQ&s",
            LicenseImageBackUrl =
                "https://inkythuatso.com/uploads/thumbnails/800/2022/08/hinh-anh-bang-lai-xe-b2-mat-sau-inkythuatso-09-15-26-07.jpg",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(4),
        },
        // Pending License Approval - Owners
        new()
        {
            Id = Guid.Parse("01960d1a-e6d7-7d48-d6b1-b6e5a8938ed8"),
            Name = "Owner Pending Approval 1",
            Email = "owner.pending1@example.com",
            Password = "Password123!".HashString(),
            Phone = "0909876543",
            Address = "101 Đường Nguyễn Hữu Thọ, Quận 7, TP.HCM",
            RoleId = Guid.Parse("01951e20-7a6e-7106-a6f3-148b63f52149"), // Owner
            LicenseNumber = "79000111111",
            LicenseImageFrontUrl =
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRlOD0DOcgJ9c2cgjGZi5sb-4VZK8bdJLYiTQ&s",
            LicenseImageBackUrl =
                "https://inkythuatso.com/uploads/thumbnails/800/2022/08/hinh-anh-bang-lai-xe-b2-mat-sau-inkythuatso-09-15-26-07.jpg",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(5),
        },
        new()
        {
            Id = Guid.Parse("01960d1a-f7c6-7d57-e7c2-c7f6b9a49fe9"),
            Name = "Owner Pending Approval 2",
            Email = "owner.pending2@example.com",
            Password = "Password123!".HashString(),
            Phone = "0903456789",
            Address = "202 Đường Mai Chí Thọ, Quận 2, TP.HCM",
            RoleId = Guid.Parse("01951e20-7a6e-7106-a6f3-148b63f52149"), // Owner
            LicenseNumber = "79000222222",
            LicenseImageFrontUrl =
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRlOD0DOcgJ9c2cgjGZi5sb-4VZK8bdJLYiTQ&s",
            LicenseImageBackUrl =
                "https://inkythuatso.com/uploads/thumbnails/800/2022/08/hinh-anh-bang-lai-xe-b2-mat-sau-inkythuatso-09-15-26-07.jpg",
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(4),
        },
    ];

    public static async Task<User[]> Execute(
        EncryptionSettings encryptionSettings,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        TokenService tokenService
    )
    {
        var userTasks = dummyUsers.Select(async u =>
        {
            string refreshToken = tokenService.GenerateRefreshToken();
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedPhone = await aesEncryptionService.Encrypt(u.Phone, key, iv);
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            EncryptionKey encryptionKeyObject = new() { EncryptedKey = encryptedKey, IV = iv };

            var user = new User()
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Address = u.Address,
                Password = u.Password,
                DateOfBirth = u.DOB,
                EncryptionKeyId = encryptionKeyObject.Id,
                Phone = encryptedPhone,
                EncryptionKey = encryptionKeyObject,
                RoleId = u.RoleId,
            };

            // Add license information for users that have it
            if (!string.IsNullOrEmpty(u.LicenseNumber))
            {
                string encryptedLicenseNumber = await aesEncryptionService.Encrypt(
                    u.LicenseNumber,
                    key,
                    iv
                );
                user.EncryptedLicenseNumber = encryptedLicenseNumber;
                user.LicenseImageFrontUrl = u.LicenseImageFrontUrl!;
                user.LicenseImageBackUrl = u.LicenseImageBackUrl!;
                user.LicenseExpiryDate = u.LicenseExpiryDate;
                user.LicenseIsApproved = null; // Pending approval
                user.LicenseRejectReason = null;
                user.LicenseImageUploadedAt = DateTimeOffset.UtcNow;
                user.LicenseApprovedAt = null;
            }

            return user;
        });
        return await Task.WhenAll(userTasks); // Await all tasks and return the array
    }
}
