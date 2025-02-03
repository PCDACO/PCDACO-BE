using Domain.Shared;

namespace Domain.Entities;

public class Manufacturer : BaseEntity
{
    public required string Name { get; set; }

    // Navigation Properties
    public ICollection<Model> Models { get; set; } = [];
}
