using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class Booking : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid CarId { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required DateTimeOffset ActualReturnTime { get; set; }
    public required decimal BasePrice { get; set; }
    public required decimal PlatformFee { get; set; }
    public required decimal ExcessDay { get; set; }
    public required decimal ExcessDayFee { get; set; }
    public required decimal TotalAmount { get; set; }
    public required string Note { get; set; }
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
    public ICollection<CarReport> CarReports { get; set; } = [];
    public ICollection<TripTracking> TripTrackings { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
}