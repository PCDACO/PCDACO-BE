using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class GPSDeviceDummyData
{
    public string OSBuildId { get; set; }
    public required string Name { get; set; }
    public required DeviceStatusEnum Status { get; set; }
}

public class GPSDeviceGenerator
{
    public static readonly GPSDeviceDummyData[] Devices =
    [
        new()
        {
            OSBuildId = "OSBuildId14",
            Name = "GPS-001",
            Status = DeviceStatusEnum.Available,
        },
        new()
        {
            OSBuildId = "OSBuildId2",
            Name = "GPS-002",
            Status = DeviceStatusEnum.Available,
        },
        new()
        {
            OSBuildId = "OSBuildId3",
            Name = "GPS-003",
            Status = DeviceStatusEnum.InUsed,
        },
        new()
        {
            OSBuildId = "OSBuildId4",
            Name = "GPS-004",
            Status = DeviceStatusEnum.Repairing,
        },
        new()
        {
            OSBuildId = "OSBuildId5",
            Name = "GPS-005",
            Status = DeviceStatusEnum.Available,
        },
        new()
        {
            OSBuildId = "OSBuildId6",
            Name = "GPS-006",
            Status = DeviceStatusEnum.InUsed,
        },
        new()
        {
            OSBuildId = "OSBuildId7",
            Name = "GPS-007",
            Status = DeviceStatusEnum.Broken,
        },
        new()
        {
            OSBuildId = "OSBuildId8",
            Name = "GPS-008",
            Status = DeviceStatusEnum.Broken,
        },
        new()
        {
            OSBuildId = "OSBuildId9",
            Name = "GPS-009",
            Status = DeviceStatusEnum.Repairing,
        },
        new()
        {
            OSBuildId = "OSBuildId11",
            Name = "Oppo 7",
            Status = DeviceStatusEnum.Available,
        },
        new()
        {
            OSBuildId = "OSBuildId12",
            Name = "GPS-011",
            Status = DeviceStatusEnum.Available,
        },
        new()
        {
            OSBuildId = "OSBuildId13",
            Name = "GPS-012",
            Status = DeviceStatusEnum.Available,
        },
        new()
        {
            OSBuildId = "OSBuildId1",
            Name = "GPS-013",
            Status = DeviceStatusEnum.Available,
        },
    ];

    public static GPSDevice[] Execute()
    {
        var devices = new List<GPSDevice>();

        foreach (var device in Devices)
        {
            devices.Add(
                new GPSDevice
                {
                    OSBuildId = device.OSBuildId,
                    Name = device.Name,
                    Status = device.Status,
                }
            );
        }
        return [.. devices];
    }
}
