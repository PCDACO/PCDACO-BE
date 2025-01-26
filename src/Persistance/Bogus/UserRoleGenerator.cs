using Domain.Entities;

namespace Persistance.Bogus;

public class UserRoleGenerator
{
    private static readonly string[] _userRoles = ["Owner", "Driver", "Admin"];
    public static UserRole[] Execute()
    {
        return [.. _userRoles.Select(status => {
            return new UserRole()
            {
                Name = status,
            };
        })];
    }
}