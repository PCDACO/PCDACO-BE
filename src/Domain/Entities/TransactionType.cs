using Domain.Shared;

namespace Domain.Entities;

public class TransactionType : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = [];
}