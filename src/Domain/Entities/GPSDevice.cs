using Domain.Enums;

using Domain.Shared;

namespace Domain.Entities;

public class GPSDevice : BaseEntity
{
    public DeviceStatusEnum Status { get; set; } = DeviceStatusEnum.Available;
    public required string Name { get; set; }
    // Navigation
    public CarGPS GPS { get; set; } = null!;

    public void Update(string name)
    {
        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}