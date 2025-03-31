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
        // new 20 cars with Available status
        new()
        {
            Color = "White",
            Seat = 5,
            FuelConsumption = 0.65M,
            Price = 35000,
            LicensePlate = "51G-12345",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Civic",
            Latitude = 10.7626728,
            Longitude = 106.6893845,
            Address = "123 Lý Tự Trọng, Phường Bến Thành, Quận 1, TP.HCM"
        },
        new()
        {
            Color = "Silver",
            Seat = 4,
            FuelConsumption = 0.55M,
            Price = 42000,
            LicensePlate = "51G-23456",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Altis",
            Latitude = 10.7889012,
            Longitude = 106.7022592,
            Address = "45 Nguyễn Thị Minh Khai, Phường Bến Nghé, Quận 1, TP.HCM"
        },
        new()
        {
            Color = "Blue",
            Seat = 7,
            FuelConsumption = 0.85M,
            Price = 50000,
            LicensePlate = "51G-34567",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Fortuner",
            Latitude = 10.7631281,
            Longitude = 106.6582099,
            Address = "72 Trần Hưng Đạo, Phường Phạm Ngũ Lão, Quận 1, TP.HCM"
        },
        new()
        {
            Color = "Gray",
            Seat = 5,
            FuelConsumption = 0.60M,
            Price = 38000,
            LicensePlate = "51G-45678",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Ranger",
            Latitude = 10.7772608,
            Longitude = 106.6987113,
            Address = "189 Cách Mạng Tháng 8, Phường 4, Quận 3, TP.HCM"
        },
        new()
        {
            Color = "Black",
            Seat = 5,
            FuelConsumption = 0.68M,
            Price = 45000,
            LicensePlate = "51G-56789",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Mazda6",
            Latitude = 10.7904874,
            Longitude = 106.6645493,
            Address = "542 Trần Hưng Đạo, Phường 2, Quận 5, TP.HCM"
        },
        new()
        {
            Color = "Red",
            Seat = 4,
            FuelConsumption = 0.58M,
            Price = 37000,
            LicensePlate = "51G-67890",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Mazda3",
            Latitude = 10.7689376,
            Longitude = 106.6926381,
            Address = "45 Võ Thị Sáu, Phường Đa Kao, Quận 1, TP.HCM"
        },
        new()
        {
            Color = "White",
            Seat = 7,
            FuelConsumption = 0.92M,
            Price = 55000,
            LicensePlate = "51G-78901",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Everest",
            Latitude = 10.7946273,
            Longitude = 106.7171683,
            Address = "210 Võ Văn Ngân, Phường Bình Thọ, TP. Thủ Đức, TP.HCM"
        },
        new()
        {
            Color = "Silver",
            Seat = 5,
            FuelConsumption = 0.62M,
            Price = 40000,
            LicensePlate = "51G-89012",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Camry",
            Latitude = 10.8020848,
            Longitude = 106.6441644,
            Address = "123 Lý Thường Kiệt, Phường 7, Quận 10, TP.HCM"
        },
        new()
        {
            Color = "Gray",
            Seat = 4,
            FuelConsumption = 0.55M,
            Price = 36000,
            LicensePlate = "51G-90123",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Vios",
            Latitude = 10.7729518,
            Longitude = 106.6579528,
            Address = "542 Điện Biên Phủ, Phường 11, Quận 10, TP.HCM"
        },
        new()
        {
            Color = "Blue",
            Seat = 7,
            FuelConsumption = 0.78M,
            Price = 48000,
            LicensePlate = "51G-01234",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Santa Fe",
            Latitude = 10.7531615,
            Longitude = 106.6287172,
            Address = "76 Nguyễn Thị Thập, Phường Tân Hưng, Quận 7, TP.HCM"
        },
        new()
        {
            Color = "Black",
            Seat = 5,
            FuelConsumption = 0.70M,
            Price = 47000,
            LicensePlate = "51G-12340",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "CR-V",
            Latitude = 10.7809623,
            Longitude = 106.6997775,
            Address = "356 Nguyễn Trãi, Phường 8, Quận 5, TP.HCM"
        },
        new()
        {
            Color = "White",
            Seat = 5,
            FuelConsumption = 0.63M,
            Price = 39000,
            LicensePlate = "51G-23450",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Accent",
            Latitude = 10.7477436,
            Longitude = 106.7017864,
            Address = "42 Nguyễn Hữu Thọ, Phường Tân Hưng, Quận 7, TP.HCM"
        },
        new()
        {
            Color = "Red",
            Seat = 7,
            FuelConsumption = 0.87M,
            Price = 52000,
            LicensePlate = "51G-34560",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Pajero",
            Latitude = 10.8276784,
            Longitude = 106.6788098,
            Address = "123 Trường Chinh, Phường 12, Quận Tân Bình, TP.HCM"
        },
        new()
        {
            Color = "Silver",
            Seat = 4,
            FuelConsumption = 0.56M,
            Price = 41000,
            LicensePlate = "51G-45670",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "K3",
            Latitude = 10.8017989,
            Longitude = 106.7513083,
            Address = "78 Đường số 7, Phường Tăng Nhơn Phú B, TP. Thủ Đức, TP.HCM"
        },
        new()
        {
            Color = "Gray",
            Seat = 5,
            FuelConsumption = 0.69M,
            Price = 44000,
            LicensePlate = "51G-56780",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Tucson",
            Latitude = 10.7337663,
            Longitude = 106.7238405,
            Address = "269 Nguyễn Đức Cảnh, Phường Tân Phong, Quận 7, TP.HCM"
        },
        new()
        {
            Color = "Blue",
            Seat = 4,
            FuelConsumption = 0.52M,
            Price = 38000,
            LicensePlate = "51G-67890",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Cerato",
            Latitude = 10.7905025,
            Longitude = 106.7025039,
            Address = "12 Trần Não, Phường An Phú, TP. Thủ Đức, TP.HCM"
        },
        new()
        {
            Color = "Black",
            Seat = 7,
            FuelConsumption = 0.88M,
            Price = 58000,
            LicensePlate = "51G-78901",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "LX570",
            Latitude = 10.8068618,
            Longitude = 106.6698242,
            Address = "456 Lê Đại Hành, Phường 11, Quận 11, TP.HCM"
        },
        new()
        {
            Color = "White",
            Seat = 5,
            FuelConsumption = 0.64M,
            Price = 43000,
            LicensePlate = "51G-89012",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Elantra",
            Latitude = 10.7618617,
            Longitude = 106.6818872,
            Address = "76 Bà Huyện Thanh Quan, Phường 6, Quận 3, TP.HCM"
        },
        new()
        {
            Color = "Silver",
            Seat = 4,
            FuelConsumption = 0.54M,
            Price = 37000,
            LicensePlate = "51G-90123",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Veloz",
            Latitude = 10.7960761,
            Longitude = 106.675864,
            Address = "123 Tô Hiến Thành, Phường 13, Quận 10, TP.HCM"
        },
        new()
        {
            Color = "Red",
            Seat = 7,
            FuelConsumption = 0.82M,
            Price = 54000,
            LicensePlate = "51G-01234",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Explorer",
            Latitude = 10.8220119,
            Longitude = 106.6295243,
            Address = "78 Cộng Hòa, Phường 4, Quận Tân Bình, TP.HCM"
        },
        new()
        {
            Color = "Black",
            Seat = 5,
            FuelConsumption = 0.59M,
            Price = 40000,
            LicensePlate = "51G-12345",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Attrage",
            Latitude = 10.7330857,
            Longitude = 106.7033653,
            Address = "34 Nguyễn Văn Linh, Phường Tân Thuận Tây, Quận 7, TP.HCM"
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
