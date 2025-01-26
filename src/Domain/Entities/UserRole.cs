using Domain.Shared;

namespace Domain.Entities;

public class UserRole : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation Properties
    public ICollection<User> Users { get; set; } = [];
}