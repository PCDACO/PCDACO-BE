using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class CarReport : BaseEntity
{
    public required Guid CarId { get; set; }
    public required Guid BookingId { get; set; }
    // Navigation Properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
    public ICollection<ImageReport> ImageReports { get; set; } = [];
}