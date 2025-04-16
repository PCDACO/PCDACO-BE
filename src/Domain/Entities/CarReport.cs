using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class CarReport : BaseEntity
{
    public Guid CarId { get; set; }
    public Guid ReportedById { get; set; }
    public required string Title { get; set; }
    public CarReportType ReportType { get; set; }
    public string Description { get; set; } = string.Empty;
    public CarReportStatus Status { get; set; } = CarReportStatus.Pending;
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedById { get; set; }
    public string? ResolutionComments { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;

    [ForeignKey(nameof(ReportedById))]
    public User ReportedBy { get; set; } = null!;

    [ForeignKey(nameof(ResolvedById))]
    public User? ResolvedBy { get; set; }

    [InverseProperty(nameof(InspectionSchedule.CarReport))]
    public InspectionSchedule? InspectionSchedule { get; set; }

    [InverseProperty(nameof(ImageReport.CarReport))]
    public ICollection<ImageReport> ImageReports { get; set; } = [];
}
