using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateUser
{
    private static User CreateUser(UserRole userRole, Guid encryptionKeyId, string email) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKeyId,
            Name = "Test User",
            Email = email,
            Password = "password",
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = "1234567890",
        };

    public static async Task<User> CreateTestUser(
        AppDBContext dBContext,
        UserRole role,
        string email = "test@example.com"
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var user = CreateUser(role, encryptionKey.Id, email);

        await dBContext.Users.AddAsync(user);
        await dBContext.SaveChangesAsync();

        return user;
    }
}
