using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class DeviceStatusGenerator
{
    private static readonly string[] _deviceStatuses = [
        DeviceStatusNames.Available,
        DeviceStatusNames.InUsed,
        DeviceStatusNames.Repairing,
        DeviceStatusNames.Broken,
        DeviceStatusNames.Removed,
    ];
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