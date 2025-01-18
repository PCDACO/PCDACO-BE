using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class Transaction : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid BookingId { get; set; }
    public decimal PlatformFee { get; set; } = 0;
    public decimal OwnerEarning { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}