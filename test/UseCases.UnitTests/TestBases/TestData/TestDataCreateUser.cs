using Domain.Entities;
using Domain.Enums;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateUser
{
    private static User CreateUser(UserRole userRole, Guid encryptionKeyId) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKeyId,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password",
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = "1234567890",
        };

    public static async Task<User> CreateTestUser(AppDBContext dBContext, UserRole role)
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var user = CreateUser(role, encryptionKey.Id);

        await dBContext.Users.AddAsync(user);
        await dBContext.SaveChangesAsync();

        return user;
    }
}
