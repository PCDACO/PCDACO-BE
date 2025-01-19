using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class ImageCar : BaseEntity
{
    // Properties
    public required Guid CarId { get; set; }
    public required string Url { get; set; }
    public ImageCarType Type { get; set; } = ImageCarType.Car;
    // Navigation Properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
}