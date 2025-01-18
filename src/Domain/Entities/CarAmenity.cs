using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class CarAmenity : BaseEntity
{
    // Properties
    public required Guid CarId { get; set; }
    public required Guid AmenityId { get; set; }
    // Navigation Properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
    [ForeignKey(nameof(AmenityId))]
    public Amenity Amenity { get; set; } = null!;
}