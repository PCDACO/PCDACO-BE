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
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedById { get; set; }
    public string? ResolutionComments { get; set; }

    // Navigation properties.
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;

    [ForeignKey(nameof(ReportedById))]
    public User ReportedBy { get; set; } = null!;

    /// <summary>
    /// Attached images or evidences as part of the report.
    /// </summary>
    public ICollection<ImageReport> ImageReports { get; set; } = [];
}
