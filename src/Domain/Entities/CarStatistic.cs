using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class CarStatistic : BaseEntity
{
    public required Guid CarId { get; set; }
    public required int TotalRented { get; set; } = 0;
    public required int TotalCancellation { get; set; } = 0;
    public required decimal TotalEarning { get; set; } = 0;
    public required decimal TotalDistance { get; set; } = 0;
    public required decimal AverageRating { get; set; } = 5;
    public required DateTimeOffset? LastRented { get; set; } = null!;
    // Navigation Properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
}