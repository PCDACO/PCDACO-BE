using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class InspectionSchedule : BaseEntity
{
    // properties
    public required Guid TechnicianId { get; set; }
    public required Guid CarId { get; set; }
    public required Guid InspectionStatusId { get; set; }
    public string Note { get; set; } = string.Empty;
    public required string InspectionAddress { get; set; }
    public required DateTimeOffset InspectionDate { get; set; }
    public required Guid CreatedBy { get; set; }

    // Navigation properties
    [InverseProperty(nameof(User.TechnicianInspectionSchedules))]
    [ForeignKey(nameof(TechnicianId))]
    public User Technician { get; set; } = null!;

    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;

    [ForeignKey(nameof(InspectionStatusId))]
    public InspectionStatus InspectionStatus { get; set; } = null!;

    [InverseProperty(nameof(User.ConsultantInspectionSchedules))]
    [ForeignKey(nameof(CreatedBy))]
    public User Consultant { get; set; } = null!;
}
