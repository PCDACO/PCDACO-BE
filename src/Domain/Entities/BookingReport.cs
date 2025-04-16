using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class BookingReport : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid ReportedById { get; set; }
    public required string Title { get; set; }
    public BookingReportType ReportType { get; set; }
    public string Description { get; set; } = string.Empty;
    public BookingReportStatus Status { get; set; } = BookingReportStatus.Pending;
    public Guid? CompensationPaidUserId { get; set; }
    public string? CompensationReason { get; set; } = string.Empty;
    public decimal? CompensationAmount { get; set; }
    public bool? IsCompensationPaid { get; set; }
    public string? CompensationPaidImageUrl { get; set; }
    public DateTimeOffset? CompensationPaidAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedById { get; set; }
    public string? ResolutionComments { get; set; }

    // Navigation properties.
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;

    [ForeignKey(nameof(ReportedById))]
    public User ReportedBy { get; set; } = null!;

    [ForeignKey(nameof(ResolvedById))]
    public User? ResolvedBy { get; set; }

    [ForeignKey(nameof(CompensationPaidUserId))]
    public User? CompensationPaidUser { get; set; }

    [InverseProperty(nameof(InspectionSchedule.Report))]
    public InspectionSchedule? InspectionSchedule { get; set; }

    [InverseProperty(nameof(ImageReport.BookingReport))]
    public ICollection<ImageReport> ImageReports { get; set; } = [];
}
