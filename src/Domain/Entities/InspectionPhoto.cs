using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class InspectionPhoto : BaseEntity
{
    public Guid? InspectionId { get; set; }
    public Guid? ScheduleId { get; set; }
    public required InspectionPhotoType Type { get; set; }
    public required string PhotoUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? InspectionCertificateExpiryDate { get; set; }

    [ForeignKey(nameof(InspectionId))]
    public CarInspection? Inspection { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public InspectionSchedule? Schedule { get; set; }
}
