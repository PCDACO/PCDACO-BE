using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateUserRole
{
    private static UserRole CreateUserRole(string roleName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = roleName };

    public static async Task<UserRole> CreateTestUserRole(AppDBContext dBContext, string roleName)
    {
        var userRole = CreateUserRole(roleName);

        await dBContext.UserRoles.AddAsync(userRole);
        await dBContext.SaveChangesAsync();

        return userRole;
    }

    public static async Task<List<UserRole>> CreateTestUserRoles(
        AppDBContext dBContext,
        List<string> roleNames
    )
    {
        var userRoles = roleNames.Select(roleName => CreateUserRole(roleName)).ToList();

        await dBContext.UserRoles.AddRangeAsync(userRoles);
        await dBContext.SaveChangesAsync();

        return userRoles;
    }

    public static async Task<List<UserRole>> InitializeTestUserRoles(AppDBContext dBContext)
    {
        List<string> roleNames = new() { "Owner", "Driver", "Admin" };

        var userRoles = new List<UserRole>();

        for (var i = 0; i < roleNames.Count; i++)
        {
            var userRole = CreateUserRole(roleNames[i]);
            userRoles.Add(userRole);
        }

        await dBContext.UserRoles.AddRangeAsync(userRoles);
        await dBContext.SaveChangesAsync();

        return userRoles;
    }
}
