using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class Transaction : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid BookingId { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Transfer;
    public decimal PlatformFee { get; set; } = 0;
    public decimal OwnerEarning { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;
    public TransactionStatus Status { get; set; } = TransactionStatus.Cancelled;
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}