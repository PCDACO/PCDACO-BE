using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class Transaction : BaseEntity
{
    public required Guid FromUserId { get; set; }
    public required Guid ToUserId { get; set; }
    public required Guid? BookingId { get; set; }
    public required Guid BankAccountId { get; set; }
    public required Guid TypeId { get; set; }
    public required Guid StatusId { get; set; }
    public decimal PlatformFee { get; set; } = 0;
    public decimal OwnerEarning { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(FromUserId))]
    [InverseProperty(nameof(User.SentTransactions))]
    public User FromUser { get; set; } = null!;

    [ForeignKey(nameof(ToUserId))]
    [InverseProperty(nameof(User.ReceivedTransactions))]
    public User ToUser { get; set; } = null!;

    [ForeignKey(nameof(TypeId))]
    public TransactionType Type { get; set; } = null!;

    [ForeignKey(nameof(StatusId))]
    public TransactionStatus Status { get; set; } = null!;

    [ForeignKey(nameof(BookingId))]
    public Booking? Booking { get; set; }

    [ForeignKey(nameof(BankAccountId))]
    public BankAccount BankAccount { get; set; } = null!;
}
