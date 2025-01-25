using Domain.Shared;

namespace Domain.Entities;

public class ContractStatus : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation properties
    public ICollection<Contract> Contracts { get; set; } = [];
}