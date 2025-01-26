using Domain.Entities;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateCar
{
    private static Car CreateCar(
        Guid ownerId,
        Guid manufacturerId,
        Guid encryptionKeyId,
        bool isDeleted = false
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = ownerId,
            ManufacturerId = manufacturerId,
            EncryptionKeyId = encryptionKeyId,
            EncryptedLicensePlate = "ABC-12345",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            PricePerDay = 100m,
            PricePerHour = 10m,
            Location = new Point(0, 0),
            IsDeleted = isDeleted
        };

    public static async Task<Car> CreateTestCar(
        AppDBContext dBContext,
        Guid ownerId,
        Guid manufacturerId,
        bool isDeleted = false
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var car = CreateCar(ownerId, manufacturerId, encryptionKey.Id, isDeleted);

        await dBContext.Cars.AddAsync(car);
        await dBContext.SaveChangesAsync();

        return car;
    }
}
