using Domain.Entities;
using Persistance.Data;
using UseCases.Utils;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateUser
{
    private static User CreateUser(
        UserRole userRole,
        Guid encryptionKeyId,
        string email,
        string AvatarUrl
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKeyId,
            Name = "Test User",
            Email = email,
            AvatarUrl = AvatarUrl,
            Password = "password".HashString(),
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
        string phone,
        string avatarUrl
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKeyId,
            Name = name,
            Email = email,
            AvatarUrl = avatarUrl,
            Password = "password".HashString(),
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = phone,
        };

    public static async Task<User> CreateTestUser(
        AppDBContext dBContext,
        UserRole role,
        string email = "test@example.com",
        string avatarUrl = "http://example.com/avatar.jpg"
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var user = CreateUser(role, encryptionKey.Id, email, avatarUrl);

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
        string phone,
        string avatarUrl = "https://www.example.com/avatar.jpg"
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);

        var user = CreateUser(role, encryptionKey.Id, email, name, phone, avatarUrl);

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
            CreateUser(
                role,
                encryptionKey1.Id,
                "user1@example.com",
                "User One",
                "1234567891",
                "https://www.example.com/avatar1.jpg"
            ),
            CreateUser(
                role,
                encryptionKey2.Id,
                "user2@example.com",
                "User Two",
                "1234567892",
                "https://www.example.com/avatar2.jpg"
            ),
            CreateUser(
                role,
                encryptionKey3.Id,
                "user3@example.com",
                "User Three",
                "1234567893",
                "https://www.example.com/avatar3.jpg"
            ),
        };

        await dBContext.Users.AddRangeAsync(users);
        await dBContext.SaveChangesAsync();

        return users;
    }
}
