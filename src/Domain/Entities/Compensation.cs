using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class Compensation : BaseEntity
{
    // Properties
    public required Guid BookingId { get; set; }
    public required Guid StatusId { get; set; }
    public required string Reason { get; set; }
    public required decimal Amount { get; set; }
    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(StatusId))]
    public CompensationStatus Status { get; set; } = null!;
}