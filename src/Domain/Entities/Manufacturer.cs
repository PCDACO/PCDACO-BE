using Domain.Shared;

namespace Domain.Entities;

public class Manufacturer : BaseEntity
{
    public required string Name { get; set; }
    public string LogoUrl { get; set; } = string.Empty;

    // Navigation Properties
    public ICollection<Model> Models { get; set; } = [];
}
