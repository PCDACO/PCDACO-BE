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
}
public class UserGenerator
{
    public static UserDummyData[] dummyUsers = [
        // ADMIN
        new(){
            Id = Guid.Parse("01951ead-d228-7d1d-9174-d9e84d69c119"),
            Name="ADMIN",
            Email="admin@gmail.com",
            Password = "admin".HashString(),
            Phone = "0931512321",
            Address = "480/59A, Đường Bình Quới, Phường 28, Quận Bình Thạnh",
            RoleId = Guid.Parse( "01951e22-c88e-7c99-901e-23ff1ebccf85"),
        },
        // DRIVER
        new(){
            Id = Guid.Parse("01950d41-d234-7b63-a360-72b27605b4a4"),
            Name="DRIVER",
            Email="thinhdpham2510@gmail.com",
            Password = "Ph@mDucThinh25102003".HashString(),
            Phone = "0938396953",
            Address = "480/59A, Đường Bình Quới, Phường 28, Quận Bình Thạnh",
            RoleId = Guid.Parse( "01951e20-ab3f-722f-aceb-3485c166e8cf"),
        },
        // Owner
        new(){
            Id = Guid.Parse("01951eae-12a7-756d-a8d5-bb1ee525d7b5"),
            Name="Owner",
            Email="thinhmusicion@gmail.com",
            Password = "Ph@mDucThinh25102003".HashString(),
            Phone = "0877344076",
            Address = "312, Đường Xô Viết Nghệ Tĩnh, Phường 25, Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e20-7a6e-7106-a6f3-148b63f52149"),
        },
        // Technician
        new(){
            Id = Guid.Parse("01951eae-453b-7ad9-949f-63dd30b592e1"),
            Name="Technician",
            Email="technician@gmail.com",
            Password = "technician".HashString(),
            Phone = "0933221132",
            Address = "480/59A Đường Bình Quới Phường 28 Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e22-ee2e-7bbf-914e-e39f14e0f420"),
        },
        // Consultant
        new(){
            Id = Guid.Parse("01951eae-7342-78fb-ae8c-02ce503ed400"),
            Name="Consultant",
            Email="thinhdpham2510@gmail.com",
            Password = "consultant".HashString(),
            Phone = "0918231512",
            Address = "480/59A Đường Bình Quới Phường 28 Quận Bình Thạnh",
            RoleId = Guid.Parse("01951e22-dd78-7933-b742-76110d88728c"),
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
            return new User()
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
        });
        return await Task.WhenAll(userTasks); // Await all tasks and return the array
    }

}