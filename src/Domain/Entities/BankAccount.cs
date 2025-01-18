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
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
}