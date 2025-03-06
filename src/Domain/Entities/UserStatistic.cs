using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class UserStatistic : BaseEntity
{
    public required Guid UserId { get; set; }
    public int TotalBooking { get; set; } = 0;
    public int TotalCompleted { get; set; } = 0;
    public int TotalRejected { get; set; } = 0;
    public int TotalExpired { get; set; } = 0;
    public int TotalCancelled { get; set; } = 0;
    public decimal TotalEarning { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0;
    public int TotalCreatedInspectionSchedule { get; set; } = 0;
    public int TotalApprovedInspectionSchedule { get; set; } = 0;
    public int TotalRejectedInspectionSchedule { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
