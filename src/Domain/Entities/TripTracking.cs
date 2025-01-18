
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class TripTracking : BaseEntity
{
    public required Guid BookingId { get; set; }
    public required decimal Latitude { get; set; }
    public required decimal Longtitude { get; set; }
    public required decimal Distance { get; set; }
    public required decimal CumulativeDistance { get; set; }
    public required decimal FuelLevel { get; set; }
    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}