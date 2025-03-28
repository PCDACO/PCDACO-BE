using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class InspectionSchedule : BaseEntity
{
    // properties
    public required Guid TechnicianId { get; set; }
    public required Guid CarId { get; set; }
    public InspectionScheduleStatusEnum Status { get; set; } = InspectionScheduleStatusEnum.Pending;
    public string Note { get; set; } = string.Empty;
    public required string InspectionAddress { get; set; }
    public required DateTimeOffset InspectionDate { get; set; }
    public required Guid CreatedBy { get; set; }
    public InspectionScheduleType Type { get; set; } = InspectionScheduleType.NewCar;
    public Guid? ReportId { get; set; }

    // Navigation properties
    [InverseProperty(nameof(User.TechnicianInspectionSchedules))]
    [ForeignKey(nameof(TechnicianId))]
    public User Technician { get; set; } = null!;

    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;

    [InverseProperty(nameof(User.ConsultantInspectionSchedules))]
    [ForeignKey(nameof(CreatedBy))]
    public User Consultant { get; set; } = null!;

    [ForeignKey(nameof(ReportId))]
    public BookingReport? Report { get; set; }

    public ICollection<InspectionPhoto> Photos { get; set; } = [];
}
