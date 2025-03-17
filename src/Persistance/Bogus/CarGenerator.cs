using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.Utils;
using UUIDNext;

namespace Persistance.Bogus;

public class CarDummyData
{
    public required string Color { get; set; }
    public required int Seat { get; set; }
    public required decimal FuelConsumption { get; set; }
    public required decimal Price { get; set; }
    public required string LicensePlate { get; set; }
    public required CarStatusEnum Status { get; set; }
    public required string FuelType { get; set; }
    public required string TransmissionType { get; set; }
    public required string Model { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required string Address { get; set; }
}

public class CarGenerator
{
    public static readonly CarDummyData[] Cars =
    [
        new()
        {
            Color = "Red",
            Seat = 5,
            FuelConsumption = 0.75M,
            Price = 30000,
            LicensePlate = "55132",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Mustang",
            Latitude = 10.7756587,
            Longitude = 106.7004238,
            Address = "268 Nam Kỳ Khởi Nghĩa, Phường 8, Quận 3, TP.HCM"
        },
        new()
        {
            Color = "Green",
            Seat = 5,
            FuelConsumption = 0.75M,
            Price = 30000,
            LicensePlate = "13622",
            Status = CarStatusEnum.Pending,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Suburban",
            Latitude = 10.8456809,
            Longitude = 106.7921667,
            Address = "Đường D1, Phường Long Thạnh Mỹ, TP. Thủ Đức, TP.HCM"
        },
        new()
        {
            Color = "Yellow",
            Seat = 9,
            FuelConsumption = 0.75M,
            Price = 30000,
            LicensePlate = "99132",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Tiguan",
            Latitude = 10.7285605,
            Longitude = 106.7218072,
            Address = "1 Nguyễn Văn Linh, Tân Phong, Quận 7, TP.HCM"
        },
        new()
        {
            Color = "Black",
            Seat = 9,
            FuelConsumption = 0.75M,
            Price = 100000,
            LicensePlate = "55555",
            Status = CarStatusEnum.Rented,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Pathfinder",
            Latitude = 10.8105831,
            Longitude = 106.7091422,
            Address = "232 Điện Biên Phủ, Phường 17, Bình Thạnh, TP.HCM"
        },
    ];

    public static async Task<Car[]> Execute(
        TransmissionType[] transmissionTypes,
        Model[] models,
        FuelType[] fuelTypes,
        EncryptionSettings encryptionSettings,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        TokenService tokenService,
        GeometryFactory geometryFactory
    )
    {
        var userTasks = Cars.Select(async u =>
        {
            string refreshToken = tokenService.GenerateRefreshToken();
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedLicensePlate = await aesEncryptionService.Encrypt(
                u.LicensePlate,
                key,
                iv
            );
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            EncryptionKey encryptionKeyObject = new() { EncryptedKey = encryptedKey, IV = iv };
            Guid newCarId = Uuid.NewDatabaseFriendly(Database.PostgreSql);

            var pickupLocation = geometryFactory.CreatePoint(
                new Coordinate(u.Longitude, u.Latitude)
            );
            pickupLocation.SRID = 4326;

            return new Car()
            {
                Id = newCarId,
                EncryptionKeyId = encryptionKeyObject.Id,
                TransmissionTypeId = transmissionTypes
                    .Where(tt => tt.Name == u.TransmissionType)
                    .Select(tt => tt.Id)
                    .First(),
                ModelId = models.Where(tt => tt.Name == u.Model).Select(tt => tt.Id).First(),
                FuelTypeId = fuelTypes
                    .Where(tt => tt.Name == u.FuelType)
                    .Select(tt => tt.Id)
                    .First(),
                Status = u.Status,
                Color = u.Color,
                EncryptedLicensePlate = encryptedLicensePlate,
                FuelConsumption = u.FuelConsumption,
                Price = u.Price,
                Seat = u.Seat,
                OwnerId = Guid.Parse("01951eae-12a7-756d-a8d5-bb1ee525d7b5"),
                EncryptionKey = encryptionKeyObject,
                PickupLocation = pickupLocation,
                PickupAddress = u.Address,
                CarStatistic = new() { CarId = newCarId, }
            };
        });
        return await Task.WhenAll(userTasks);
    }
}
