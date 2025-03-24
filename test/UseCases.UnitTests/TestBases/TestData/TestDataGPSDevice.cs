using Domain.Entities;
using Domain.Enums;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataGPSDevice
{
    public static async Task<GPSDevice> CreateTestGPSDevice(
        AppDBContext dBContext,
        string name = "Test GPS Device",
        DeviceStatusEnum status = DeviceStatusEnum.Available,
        bool isDeleted = false
    )
    {
        var device = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = name,
            Status = status,
            IsDeleted = isDeleted
        };

        await dBContext.GPSDevices.AddAsync(device);
        await dBContext.SaveChangesAsync();

        return device;
    }
}
