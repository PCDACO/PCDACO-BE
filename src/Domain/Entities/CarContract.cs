using Domain.Shared;

namespace Domain.Entities;

public class CarContract : BaseEntity
{
    public required Guid CarId { get; set; }
    public string Terms { get; set; } = string.Empty;
    // Navigation 
    public Car Car { get; set; } = null!;
}