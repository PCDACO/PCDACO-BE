using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class Withdrawal : BaseEntity
{
    public required Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}