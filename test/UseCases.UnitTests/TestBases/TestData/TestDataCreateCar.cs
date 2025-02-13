using Domain.Entities;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateCar
{
    private static Car CreateCar(
        Guid ownerId,
        Guid modelId,
        Guid encryptionKeyId,
        TransmissionType transmissionType,
        FuelType fuelType,
        CarStatus carStatus,
        bool isDeleted = false
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = ownerId,
            ModelId = modelId,
            EncryptionKeyId = encryptionKeyId,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmissionType.Id,
            StatusId = carStatus.Id,
            EncryptedLicensePlate = "ABC-12345",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            PricePerDay = 100m,
            PricePerHour = 10m,
            Location = new Point(0, 0),
            IsDeleted = isDeleted,
        };

    public static async Task<Car> CreateTestCar(
        AppDBContext dBContext,
        Guid ownerId,
        Guid modelId,
        TransmissionType transmissionType,
        FuelType fuelType,
        CarStatus carStatus,
        bool isDeleted = false
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(dBContext);
        var car = CreateCar(
            ownerId: ownerId,
            modelId: modelId,
            encryptionKeyId: encryptionKey.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus,
            isDeleted: isDeleted
        );

        var carStatistic = new CarStatistic { CarId = car.Id };

        await dBContext.Cars.AddAsync(car);
        await dBContext.CarStatistics.AddAsync(carStatistic);
        await dBContext.SaveChangesAsync();

        return car;
    }
}
