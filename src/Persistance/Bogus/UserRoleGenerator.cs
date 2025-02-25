using Domain.Entities;

namespace Persistance.Bogus;

public class UserRoleDummyData
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}
public class UserRoleGenerator
{
    private static readonly UserRoleDummyData[] _userRoles = [
        new(){
            Id = Guid.Parse("01951e20-7a6e-7106-a6f3-148b63f52149"),
            Name = "Owner",
        },
        new(){
            Id = Guid.Parse("01951e20-ab3f-722f-aceb-3485c166e8cf"),
            Name = "Driver",
        },
        new(){
            Id = Guid.Parse("01951e22-c88e-7c99-901e-23ff1ebccf85"),
            Name = "Admin",
        },
        new(){
            Id = Guid.Parse("01951e22-dd78-7933-b742-76110d88728c"),
            Name = "Consultant",
        },
        new(){
            Id = Guid.Parse("01951e22-ee2e-7bbf-914e-e39f14e0f420"),
            Name = "Technician",
        },
    ];
    public static UserRole[] Execute()
    {
        return [.. _userRoles.Select(status => {
            return new UserRole()
            {
                Id = status.Id,
                Name = status.Name
            };
        })];
    }
}