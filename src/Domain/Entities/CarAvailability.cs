using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class CarAvailability : BaseEntity
{
    // Properties
    public Guid CarId { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsAvailable { get; set; } = true;

    // Navigation Properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
}
