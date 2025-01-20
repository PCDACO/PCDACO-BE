using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class CarStatistic : BaseEntity
{
    public required Guid CarId { get; set; }
    public int TotalRented { get; set; } = 0;
    public int TotalCancellation { get; set; } = 0;
    public decimal TotalEarning { get; set; } = 0;
    public decimal TotalDistance { get; set; } = 0;
    public decimal AverageRating { get; set; } = 5;
    public DateTimeOffset? LastRented { get; set; } = null!;
    // Navigation Properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
}