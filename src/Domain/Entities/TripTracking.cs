using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;
using NetTopologySuite.Geometries;

namespace Domain.Entities;

public class TripTracking : BaseEntity
{
    public required Guid BookingId { get; set; }
    public required Point Location { get; set; }
    public required decimal Distance { get; set; }
    public required decimal CumulativeDistance { get; set; }

    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}
