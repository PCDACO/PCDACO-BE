using Domain.Shared;

namespace Domain.Entities;

public class InspectionStatus : BaseEntity
{
    // properties
    public required string Name { get; set; }
    // Navigation properties
    public ICollection<InspectionSchedule> InspectingSchedules { get; set; } = [];
}