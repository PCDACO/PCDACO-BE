using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class Booking : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid CarId { get; set; }
    public BookingStatusEnum Status { get; set; } = BookingStatusEnum.Pending;
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required DateTimeOffset ActualReturnTime { get; set; }
    public required decimal BasePrice { get; set; }
    public required decimal PlatformFee { get; set; }
    public required decimal ExcessDay { get; set; }
    public required decimal ExcessDayFee { get; set; }
    public required decimal TotalAmount { get; set; }
    public decimal TotalDistance { get; set; } = 0;
    public required string Note { get; set; }
    public bool IsCarReturned { get; set; } = true;
    public long? PayOSOrderCode { get; set; }
    public bool IsPaid { get; set; } = false;
    public bool IsRefund { get; set; } = false;
    public decimal? RefundAmount { get; set; } = null;
    public DateTimeOffset? RefundDate { get; set; } = null;
    public decimal? ExtensionAmount { get; set; }
    public bool? IsExtensionPaid { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
    public Contract Contract { get; set; } = null!;

    [InverseProperty(nameof(BookingReport.Booking))]
    public ICollection<BookingReport> BookingReports { get; set; } = [];
    public ICollection<TripTracking> TripTrackings { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<CarInspection> CarInspections { get; set; } = [];
}
