using Domain.Entities;

namespace Persistance.Bogus;

public class DeviceStatusGenerator
{
    private static readonly string[] _deviceStatuses = ["Available", "In Used", "Repairing", "Broken", "Removed"];
    public static DeviceStatus[] Execute()
    {
        return [.. _deviceStatuses.Select(status => {
            return new DeviceStatus()
            {
                Name = status,
            };
        })];
    }
}