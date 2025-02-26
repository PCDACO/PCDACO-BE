using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class GPSDeviceDummyData
{
    public required string Name { get; set; }
    public required string Status { get; set; }
}

public class GPSDeviceGenerator
{
    public static readonly GPSDeviceDummyData[] Devices =
    [
        new() { Name = "GPS-001", Status = DeviceStatusNames.Available },
        new() { Name = "GPS-002", Status = DeviceStatusNames.Available },
        new() { Name = "GPS-003", Status = DeviceStatusNames.InUsed },
        new() { Name = "GPS-004", Status = DeviceStatusNames.Repairing },
        new() { Name = "GPS-005", Status = DeviceStatusNames.Available },
        new() { Name = "GPS-006", Status = DeviceStatusNames.InUsed },
        new() { Name = "GPS-007", Status = DeviceStatusNames.Broken },
        new() { Name = "GPS-008", Status = DeviceStatusNames.Broken },
        new() { Name = "GPS-009", Status = DeviceStatusNames.Repairing },
        new() { Name = "GPS-010", Status = DeviceStatusNames.Available },
    ];

    public static GPSDevice[] Execute(DeviceStatus[] statuses)
    {
        var devices = new List<GPSDevice>();

        foreach (var device in Devices)
        {
            Guid statusId = statuses.Where(s => s.Name == device.Status).Select(s => s.Id).First();

            devices.Add(new GPSDevice { Name = device.Name, StatusId = statusId });
        }

        return [.. devices];
    }
}
