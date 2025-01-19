using Domain.Shared;

namespace Domain.Entities;

public class Amenity : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    public required string Description { get; set; }
    // Navigation Properties
    public ICollection<CarAmenity> CarAmenities { get; set; } = [];
}