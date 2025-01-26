using Domain.Shared;

namespace Domain.Entities;

public class CompensationStatus : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation properties
    public ICollection<Compensation> Compensations { get; set; } = [];
}