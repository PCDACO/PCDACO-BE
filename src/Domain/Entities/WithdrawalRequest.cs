using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class WithdrawalRequest : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid BankAccountId { get; set; }
    public WithdrawRequestStatusEnum Status { get; set; } = WithdrawRequestStatusEnum.Pending;
    public required decimal Amount { get; set; }
    public string RejectReason { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(BankAccountId))]
    public BankAccount BankAccount { get; set; } = null!;
}