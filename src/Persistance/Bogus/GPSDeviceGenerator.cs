using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class GPSDeviceDummyData
{
    public required string Name { get; set; }
    public required DeviceStatusEnum Status { get; set; }
}

public class GPSDeviceGenerator
{
    public static readonly GPSDeviceDummyData[] Devices =
    [
        new() { Name = "GPS-001", Status = DeviceStatusEnum.Available },
        new() { Name = "GPS-002", Status = DeviceStatusEnum.Available },
        new() { Name = "GPS-003", Status = DeviceStatusEnum.InUsed },
        new() { Name = "GPS-004", Status = DeviceStatusEnum.Repairing },
        new() { Name = "GPS-005", Status = DeviceStatusEnum.Available },
        new() { Name = "GPS-006", Status = DeviceStatusEnum.InUsed },
        new() { Name = "GPS-007", Status = DeviceStatusEnum.Broken },
        new() { Name = "GPS-008", Status = DeviceStatusEnum.Broken },
        new() { Name = "GPS-009", Status = DeviceStatusEnum.Repairing },
        new() { Name = "GPS-010", Status = DeviceStatusEnum.Available },
    ];

    public static GPSDevice[] Execute()
    {
        var devices = new List<GPSDevice>();

        foreach (var device in Devices)
        {
            devices.Add(new GPSDevice { Name = device.Name, Status = device.Status });
        }
        return [.. devices];
    }
}