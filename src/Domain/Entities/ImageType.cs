using Domain.Shared;

namespace Domain.Entities;

public class ImageType : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation Properties
    public ICollection<ImageCar> Images { get; set; } = [];
}