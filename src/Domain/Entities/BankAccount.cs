using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class BankAccount : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid EncryptionKeyId { get; set; }
    public required string BankName { get; set; }
    public required string EncryptedBankAccount { get; set; }
    public required string BankAccountName { get; set; }
    public required bool IsPrimary { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = [];
}
