using Domain.Entities;

namespace Domain.Data;

public class DeviceStatusesData()
{
    public ICollection<DeviceStatus> Statuses { get; private set; } = [];
    public void SetStatuses(DeviceStatus[] statuses)
        => Statuses = statuses;
}