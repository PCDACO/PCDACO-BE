using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class CarInspection : BaseEntity
{
    public required Guid BookingId { get; set; }
    public required InspectionType Type { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsComplete { get; set; }

    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
    public ICollection<InspectionPhoto> Photos { get; set; } = [];
}
