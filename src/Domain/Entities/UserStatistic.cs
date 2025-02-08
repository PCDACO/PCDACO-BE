using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class UserStatistic : BaseEntity
{
    public required Guid UserId { get; set; }
    public int TotalBooking { get; set; } = 0;
    public int TotalCancel { get; set; } = 0;
    public decimal TotalEarning { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0;
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
