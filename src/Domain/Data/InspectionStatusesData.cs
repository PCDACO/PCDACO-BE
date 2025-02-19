using Domain.Entities;

namespace Domain.Data;

public class InspectionStatusesData
{
    public ICollection<InspectionStatus> Statuses { get; private set; } = [];

    public void SetStatuses(InspectionStatus[] statuses) => Statuses = statuses;
}
