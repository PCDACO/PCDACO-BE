using Domain.Shared;

namespace Domain.Entities;

public class TransmissionType : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation properties
    public ICollection<Car>? Car { get; set; } = null;
}