using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Shared;

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
    public required string Status { get; set; }
    public required string FuelType { get; set; }
    public required string TransmissionType { get; set; }
    public required string Model { get; set; }
}
public class CarGenerator
{
    public static readonly CarDummyData[] Cars = [
        new(){
            Color = "Red",
            Seat = 5,
            FuelConsumption = 0.75M,
            Price = 30000,
            LicensePlate = "55132",
            Status = CarStatusNames.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Mustang"
        },
        new(){
            Color = "Green",
            Seat = 5,
            FuelConsumption = 0.75M,
            Price = 30000,
            LicensePlate = "13622",
            Status = CarStatusNames.Pending,
            FuelType = FuelTypeNames.Gasoline,
            TransmissionType = TransmissionTypeNames.Manual,
            Model = "Suburban"
        },
        new(){
            Color = "Yellow",
            Seat = 9,
            FuelConsumption = 0.75M,
            Price = 30000,
            LicensePlate = "99132",
            Status = CarStatusNames.Available,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Tiguan"
        },
        new(){
            Color = "Black",
            Seat = 9,
            FuelConsumption = 0.75M,
            Price = 100000,
            LicensePlate = "55555",
            Status = CarStatusNames.Rented,
            FuelType = FuelTypeNames.Diesel,
            TransmissionType = TransmissionTypeNames.Automatic,
            Model = "Pathfinder"
        },
    ];
    public static async Task<Car[]> Execute(
        TransmissionType[] transmissionTypes,
        Model[] models,
        FuelType[] fuelTypes,
        CarStatus[] carStatuses,
        EncryptionSettings encryptionSettings,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        TokenService tokenService
    )
    {
        var userTasks = Cars.Select(async u =>
{
    string refreshToken = tokenService.GenerateRefreshToken();
    (string key, string iv) = await keyManagementService.GenerateKeyAsync();
    string encryptedLicensePlate = await aesEncryptionService.Encrypt(u.LicensePlate, key, iv);
    string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
    EncryptionKey encryptionKeyObject = new() { EncryptedKey = encryptedKey, IV = iv };
    Guid newCarId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
    return new Car()
    {
        Id = newCarId,
        EncryptionKeyId = encryptionKeyObject.Id,
        TransmissionTypeId = transmissionTypes.Where(tt => tt.Name == u.TransmissionType).Select(tt => tt.Id).First(),
        ModelId = models.Where(tt => tt.Name == u.Model).Select(tt => tt.Id).First(),
        FuelTypeId = fuelTypes.Where(tt => tt.Name == u.FuelType).Select(tt => tt.Id).First(),
        StatusId = carStatuses.Where(cs => cs.Name == u.Status).Select(cs => cs.Id).First(),
        Color = u.Color,
        EncryptedLicensePlate = encryptedLicensePlate,
        FuelConsumption = u.FuelConsumption,
        Price = u.Price,
        Seat = u.Seat,
        OwnerId = Guid.Parse("01951eae-12a7-756d-a8d5-bb1ee525d7b5"),
        EncryptionKey = encryptionKeyObject,
        CarStatistic = new()
        {
            CarId = newCarId,
        }
    };
});
        return await Task.WhenAll(userTasks); // Await all tasks and return the array
    }
}