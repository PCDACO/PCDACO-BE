using Domain.Shared;

namespace Domain.Entities;

public class DeviceStatus : BaseEntity
{
    public required string Name { get; set; }
    // Navigation
    public ICollection<GPSDevice> Devices { get; set; } = [];
}