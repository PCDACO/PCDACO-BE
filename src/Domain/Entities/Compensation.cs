using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class Compensation : BaseEntity
{
    // Properties
    public required Guid BookingId { get; set; }
    public CompensationStatusEnum Status { get; set; } = CompensationStatusEnum.Pending;
    public required string Reason { get; set; }
    public required decimal Amount { get; set; }
    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}