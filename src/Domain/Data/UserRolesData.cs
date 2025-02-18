using Domain.Entities;

namespace Domain.Data;

public class UserRolesData
{
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    public void Set(UserRole[] roles) => UserRoles = roles;
}
