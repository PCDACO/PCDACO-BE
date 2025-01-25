using Domain.Shared;

namespace Domain.Entities;

public class FuelType : BaseEntity
{
    // Properties
    public required string Name { get; set; } = string.Empty;
    // Navigation property
    public ICollection<Car> Cars { get; set; } = [];
}