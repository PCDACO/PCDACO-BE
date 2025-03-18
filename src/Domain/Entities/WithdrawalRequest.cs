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
    public Guid? TransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedByAdminId { get; set; }
    public string? AdminNote { get; set; }
    public string WithdrawalCode { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.WithdrawalRequests))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(BankAccountId))]
    public BankAccount BankAccount { get; set; } = null!;

    [ForeignKey(nameof(TransactionId))]
    public Transaction? Transaction { get; set; }

    [ForeignKey(nameof(ProcessedByAdminId))]
    public User? ProcessedByAdmin { get; set; }
}
