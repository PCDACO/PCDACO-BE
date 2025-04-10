using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateCar
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    // Default test location (example: Ho Chi Minh City center)
    private const double DEFAULT_LATITUDE = 10.7756587;
    private const double DEFAULT_LONGITUDE = 106.7004238;
    private const string DEFAULT_ADDRESS = "268 Nam Kỳ Khởi Nghĩa, Phường 8, Quận 3, TP.HCM";

    private static Car CreateCar(
        Guid ownerId,
        Guid modelId,
        TransmissionType transmissionType,
        FuelType fuelType,
        CarStatusEnum carStatus,
        bool isDeleted = false,
        double latitude = DEFAULT_LATITUDE,
        double longitude = DEFAULT_LONGITUDE,
        string address = DEFAULT_ADDRESS
    )
    {
        var pickupLocation = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        pickupLocation.SRID = 4326;

        return new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = ownerId,
            ModelId = modelId,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmissionType.Id,
            Status = carStatus,
            LicensePlate = "ABC-12345",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            Price = 100m,
            PickupLocation = pickupLocation,
            PickupAddress = address,
            IsDeleted = isDeleted,
        };
    }

    private static async Task<CarGPS> CreateCarGPS(
        AppDBContext dBContext,
        Guid carId,
        double latitude = DEFAULT_LATITUDE,
        double longitude = DEFAULT_LONGITUDE
    )
    {
        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(dBContext);
        var location = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        location.SRID = 4326;

        var carGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DeviceId = gpsDevice.Id,
            CarId = carId,
            Location = location,
            IsDeleted = false
        };

        await dBContext.CarGPSes.AddAsync(carGPS);
        await dBContext.SaveChangesAsync();

        return carGPS;
    }

    public static async Task<Car> CreateTestCar(
        AppDBContext dBContext,
        Guid ownerId,
        Guid modelId,
        TransmissionType transmissionType,
        FuelType fuelType,
        CarStatusEnum carStatus,
        bool isDeleted = false,
        double latitude = DEFAULT_LATITUDE,
        double longitude = DEFAULT_LONGITUDE,
        string address = DEFAULT_ADDRESS
    )
    {
        var car = CreateCar(
            ownerId: ownerId,
            modelId: modelId,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus,
            isDeleted: isDeleted,
            latitude: latitude,
            longitude: longitude,
            address: address
        );

        var carStatistic = new CarStatistic { CarId = car.Id };

        await dBContext.Cars.AddAsync(car);
        await dBContext.CarStatistics.AddAsync(carStatistic);
        await dBContext.SaveChangesAsync();

        // Create GPS data for the car
        await CreateCarGPS(dBContext, car.Id, latitude, longitude);

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
        bool isDeleted = false,
        double latitude = DEFAULT_LATITUDE,
        double longitude = DEFAULT_LONGITUDE,
        string address = DEFAULT_ADDRESS
    )
    {
        var car = await CreateTestCarHasValidEncryption(
            dBContext,
            ownerId,
            modelId,
            transmissionType,
            fuelType,
            (CarStatusEnum)Enum.Parse(typeof(CarStatusEnum), carStatus),
            isDeleted,
            latitude,
            longitude,
            address
        );

        var imageType = await GetOrCreateCarImageType(dBContext);

        // Create and add images
        int i = 0;
        var carImages = imageUrls
            .Select(url => new ImageCar
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                TypeId = imageType.Id,
                Url = url,
                Name = $"Car-{car.Id}-Image-{url}-{i++}",
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
        bool isDeleted = false,
        double latitude = DEFAULT_LATITUDE,
        double longitude = DEFAULT_LONGITUDE,
        string address = DEFAULT_ADDRESS
    )
    {
        // Generate encryption key and encrypt license plate
        string licensePlate = "ABC-12345";

        // Create pickup location point
        var pickupLocation = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        pickupLocation.SRID = 4326;

        // Create car with proper encryption
        Guid carId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        var car = new Car
        {
            Id = carId,
            OwnerId = ownerId,
            ModelId = modelId,
            LicensePlate = licensePlate,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmissionType.Id,
            Status = carStatus,
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            Price = 100m,
            PickupLocation = pickupLocation,
            PickupAddress = address,
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
