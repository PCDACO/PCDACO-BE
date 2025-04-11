using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class BookingLockedBalance : BaseEntity
{
    public required Guid BookingId { get; set; }
    public required Guid OwnerId { get; set; }
    public required decimal Amount { get; set; }

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;

    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; set; } = null!;
}
