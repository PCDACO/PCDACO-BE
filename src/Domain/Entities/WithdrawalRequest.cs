using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class WithdrawalRequest : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid BankAccountId { get; set; }
    public required Guid StatusId { get; set; }
    public required decimal Amount { get; set; }
    public string RejectReason { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(StatusId))]
    public WithdrawalRequestStatus Status { get; set; } = null!;

    [ForeignKey(nameof(BankAccountId))]
    public BankAccount BankAccount { get; set; } = null!;
}
