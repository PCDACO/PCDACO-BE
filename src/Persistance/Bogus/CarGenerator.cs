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
            Address = "268 Nam Kỳ Khởi Nghĩa, Phường 8, Quận 3, TP.HCM",
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
            Address = "Đường D1, Phường Long Thạnh Mỹ, TP. Thủ Đức, TP.HCM",
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
            Address = "1 Nguyễn Văn Linh, Tân Phong, Quận 7, TP.HCM",
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
            Address = "232 Điện Biên Phủ, Phường 17, Bình Thạnh, TP.HCM",
        },
        // 20 new cars with Available status
        new()
        {
            Color = "White",
            Seat = 5,
            FuelConsumption = 0.65M,
            Price = 45000,
            LicensePlate = "51A-12345",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Camry",
            Latitude = 10.7825,
            Longitude = 106.6958,
            Address = "123 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM",
        },
        new()
        {
            Color = "Silver",
            Seat = 7,
            FuelConsumption = 0.78M,
            Price = 50000,
            LicensePlate = "51A-23456",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Fortuner",
            Latitude = 10.7714,
            Longitude = 106.6977,
            Address = "45 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM",
        },
        new()
        {
            Color = "Blue",
            Seat = 4,
            FuelConsumption = 0.55M,
            Price = 38000,
            LicensePlate = "51A-34567",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Electric,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Model 3",
            Latitude = 10.7632,
            Longitude = 106.6822,
            Address = "76 Hai Bà Trưng, Phường Bến Nghé, Quận 1, TP.HCM",
        },
        new()
        {
            Color = "Gray",
            Seat = 5,
            FuelConsumption = 0.62M,
            Price = 42000,
            LicensePlate = "51A-45678",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Hybrid,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Civic",
            Latitude = 10.7880,
            Longitude = 106.7024,
            Address = "215 Điện Biên Phủ, Phường 15, Bình Thạnh, TP.HCM",
        },
        new()
        {
            Color = "Pearl White",
            Seat = 7,
            FuelConsumption = 0.75M,
            Price = 55000,
            LicensePlate = "51A-56789",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Santa Fe",
            Latitude = 10.8016,
            Longitude = 106.7133,
            Address = "348 Võ Văn Ngân, Phường Bình Thọ, TP. Thủ Đức, TP.HCM",
        },
        new()
        {
            Color = "Maroon",
            Seat = 5,
            FuelConsumption = 0.68M,
            Price = 48000,
            LicensePlate = "51A-67890",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "3 Series",
            Latitude = 10.7529,
            Longitude = 106.7234,
            Address = "90 Nguyễn Hữu Cảnh, Phường 22, Bình Thạnh, TP.HCM",
        },
        new()
        {
            Color = "Black",
            Seat = 5,
            FuelConsumption = 0.60M,
            Price = 60000,
            LicensePlate = "51A-78901",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Hybrid,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "A4",
            Latitude = 10.7725,
            Longitude = 106.7031,
            Address = "150 Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP.HCM",
        },
        new()
        {
            Color = "Champagne",
            Seat = 7,
            FuelConsumption = 0.80M,
            Price = 52000,
            LicensePlate = "51A-89012",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Everest",
            Latitude = 10.7982,
            Longitude = 106.6763,
            Address = "235 Hoàng Văn Thụ, Phường 8, Phú Nhuận, TP.HCM",
        },
        new()
        {
            Color = "Dark Green",
            Seat = 9,
            FuelConsumption = 0.85M,
            Price = 65000,
            LicensePlate = "51A-90123",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Suburban",
            Latitude = 10.8014,
            Longitude = 106.6469,
            Address = "120 Phan Xích Long, Phường 2, Phú Nhuận, TP.HCM",
        },
        new()
        {
            Color = "Navy Blue",
            Seat = 5,
            FuelConsumption = 0.70M,
            Price = 40000,
            LicensePlate = "51A-01234",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Focus",
            Latitude = 10.7311,
            Longitude = 106.7315,
            Address = "1060 Nguyễn Văn Linh, Tân Phong, Quận 7, TP.HCM",
        },
        new()
        {
            Color = "Burgundy",
            Seat = 5,
            FuelConsumption = 0.64M,
            Price = 45000,
            LicensePlate = "51A-12300",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Electric,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Model Y",
            Latitude = 10.7212,
            Longitude = 106.7195,
            Address = "72 Lê Văn Lương, Tân Quy, Quận 7, TP.HCM",
        },
        new()
        {
            Color = "Beige",
            Seat = 5,
            FuelConsumption = 0.67M,
            Price = 38000,
            LicensePlate = "51A-23400",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Hybrid,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Corolla",
            Latitude = 10.7954,
            Longitude = 106.7248,
            Address = "200 Xô Viết Nghệ Tĩnh, Phường 21, Bình Thạnh, TP.HCM",
        },
        new()
        {
            Color = "Dark Gray",
            Seat = 7,
            FuelConsumption = 0.75M,
            Price = 58000,
            LicensePlate = "51A-34500",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Ranger",
            Latitude = 10.8482,
            Longitude = 106.7808,
            Address = "15 Võ Văn Ngân, Linh Chiểu, TP. Thủ Đức, TP.HCM",
        },
        new()
        {
            Color = "Metallic Blue",
            Seat = 5,
            FuelConsumption = 0.63M,
            Price = 47000,
            LicensePlate = "51A-45600",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "5 Series",
            Latitude = 10.7868,
            Longitude = 106.8045,
            Address = "300 Đỗ Xuân Hợp, Phước Long B, TP. Thủ Đức, TP.HCM",
        },
        new()
        {
            Color = "Graphite",
            Seat = 7,
            FuelConsumption = 0.72M,
            Price = 54000,
            LicensePlate = "51A-56700",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "CR-V",
            Latitude = 10.8305,
            Longitude = 106.7903,
            Address = "8 Lê Văn Chí, Linh Trung, TP. Thủ Đức, TP.HCM",
        },
        new()
        {
            Color = "Cherry Red",
            Seat = 5,
            FuelConsumption = 0.66M,
            Price = 42000,
            LicensePlate = "51A-67800",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Hybrid,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Accord",
            Latitude = 10.7125,
            Longitude = 106.7257,
            Address = "489 Huỳnh Tấn Phát, Tân Thuận Đông, Quận 7, TP.HCM",
        },
        new()
        {
            Color = "Olive Green",
            Seat = 5,
            FuelConsumption = 0.65M,
            Price = 39000,
            LicensePlate = "51A-78900",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Elantra",
            Latitude = 10.7548,
            Longitude = 106.6545,
            Address = "196 Phạm Văn Hai, Phường 5, Tân Bình, TP.HCM",
        },
        new()
        {
            Color = "Midnight Blue",
            Seat = 7,
            FuelConsumption = 0.70M,
            Price = 51000,
            LicensePlate = "51A-89100",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Palisade",
            Latitude = 10.7733,
            Longitude = 106.6576,
            Address = "356 Cộng Hòa, Phường 13, Tân Bình, TP.HCM",
        },
        new()
        {
            Color = "Platinum",
            Seat = 5,
            FuelConsumption = 0.58M,
            Price = 44000,
            LicensePlate = "51A-90120",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Electric,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Model S",
            Latitude = 10.7558,
            Longitude = 106.6416,
            Address = "78 Lý Thường Kiệt, Phường 7, Tân Bình, TP.HCM",
        },
        new()
        {
            Color = "Charcoal",
            Seat = 5,
            FuelConsumption = 0.68M,
            Price = 49000,
            LicensePlate = "51A-01230",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Hybrid,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "ES",
            Latitude = 10.8192,
            Longitude = 106.6291,
            Address = "51 Quang Trung, Phường 10, Gò Vấp, TP.HCM",
        },
        new()
        {
            Color = "Ruby Red",
            Seat = 7,
            FuelConsumption = 0.73M,
            Price = 53000,
            LicensePlate = "51A-22334",
            Status = CarStatusEnum.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Explorer",
            Latitude = 10.8348,
            Longitude = 106.6452,
            Address = "200 Nguyễn Văn Lượng, Phường 17, Gò Vấp, TP.HCM",
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
