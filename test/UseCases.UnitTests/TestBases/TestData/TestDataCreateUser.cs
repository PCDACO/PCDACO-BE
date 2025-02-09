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

    private static User CreateUser(
        UserRole userRole,
        Guid encryptionKeyId,
        string email,
        string name,
        string phone
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKeyId,
            Name = name,
            Email = email,
            Password = "password",
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = phone,
        };

    public static async Task<User> CreateTestUser(
        AppDBContext dBContext,
        UserRole role,
        string email = "test@example.com"
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var user = CreateUser(role, encryptionKey.Id, email);

        var userStatistic = new UserStatistic { UserId = user.Id };

        await dBContext.Users.AddAsync(user);
        await dBContext.UserStatistics.AddAsync(userStatistic);
        await dBContext.SaveChangesAsync();

        return user;
    }

    public static async Task<User> CreateTestUser(
        AppDBContext dBContext,
        UserRole role,
        string email,
        string name,
        string phone
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var user = CreateUser(role, encryptionKey.Id, email, name, phone);

        await dBContext.Users.AddAsync(user);
        await dBContext.SaveChangesAsync();

        return user;
    }

    public static async Task<List<User>> CreateTestUserList(AppDBContext dBContext, UserRole role)
    {
        var encryptionKey1 = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var encryptionKey2 = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var encryptionKey3 = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var users = new List<User>
        {
            CreateUser(role, encryptionKey1.Id, "user1@example.com", "User One", "1234567891"),
            CreateUser(role, encryptionKey2.Id, "user2@example.com", "User Two", "1234567892"),
            CreateUser(role, encryptionKey3.Id, "user3@example.com", "User Three", "1234567893"),
        };

        await dBContext.Users.AddRangeAsync(users);
        await dBContext.SaveChangesAsync();

        return users;
    }
}
