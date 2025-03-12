using Domain.Entities;
using Domain.Enums;
using Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Persistance.Data;

using UseCases.Abstractions;

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
        CarStatusEnum carStatus,
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
            Status = carStatus,
            EncryptedLicensePlate = "ABC-12345",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            Price = 100m,
            IsDeleted = isDeleted,
        };

    public static async Task<Car> CreateTestCar(
        AppDBContext dBContext,
        Guid ownerId,
        Guid modelId,
        TransmissionType transmissionType,
        FuelType fuelType,
        CarStatusEnum carStatus,
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

    public static async Task<Car> CreateTestCarWithImages(
        AppDBContext dBContext,
        Guid ownerId,
        Guid modelId,
        TransmissionType transmissionType,
        FuelType fuelType,
        string carStatus,
        string[] imageUrls,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        bool isDeleted = false
    )
    {
        var car = await CreateTestCarHasValidEncryption(
            dBContext,
            ownerId,
            modelId,
            transmissionType,
            fuelType,
            (CarStatusEnum)Enum.Parse(typeof(CarStatusEnum), carStatus),
            aesEncryptionService,
            keyManagementService,
            encryptionSettings,
            isDeleted
        );

        var imageType = await GetOrCreateCarImageType(dBContext);

        // Create and add images
        var carImages = imageUrls
            .Select(url => new ImageCar
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                TypeId = imageType.Id,
                Url = url,
                IsDeleted = false,
            })
            .ToList();

        await dBContext.ImageCars.AddRangeAsync(carImages);
        await dBContext.SaveChangesAsync();

        return car;
    }

    public static async Task<Car> CreateTestCarHasValidEncryption(
        AppDBContext dBContext,
        Guid ownerId,
        Guid modelId,
        TransmissionType transmissionType,
        FuelType fuelType,
        CarStatusEnum carStatus,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        bool isDeleted = false
    )
    {
        // Generate encryption key and encrypt license plate
        (string key, string iv) = await keyManagementService.GenerateKeyAsync();
        string licensePlate = "ABC-12345";
        string encryptedLicensePlate = await aesEncryptionService.Encrypt(licensePlate, key, iv);
        string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);

        // Create encryption key
        var newEncryptionKey = new EncryptionKey
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptedKey = encryptedKey,
            IV = iv,
        };
        await dBContext.EncryptionKeys.AddAsync(newEncryptionKey);
        await dBContext.SaveChangesAsync();

        // Create car with proper encryption
        Guid carId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        var car = new Car
        {
            Id = carId,
            OwnerId = ownerId,
            ModelId = modelId,
            EncryptionKeyId = newEncryptionKey.Id,
            EncryptedLicensePlate = encryptedLicensePlate,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmissionType.Id,
            Status = carStatus,
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            Price = 100m,
            IsDeleted = isDeleted,
        };

        await dBContext.Cars.AddAsync(car);
        await dBContext.SaveChangesAsync();

        return car;
    }

    private static async Task<ImageType> GetOrCreateCarImageType(AppDBContext dbContext)
    {
        var imageType = await dbContext.ImageTypes.FirstOrDefaultAsync(t => t.Name == "Car");

        if (imageType == null)
        {
            imageType = new ImageType
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Car",
                IsDeleted = false,
            };
            await dbContext.ImageTypes.AddAsync(imageType);
            await dbContext.SaveChangesAsync();
        }

        return imageType;
    }
}