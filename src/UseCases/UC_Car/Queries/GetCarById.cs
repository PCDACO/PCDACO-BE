using System.Threading.Tasks;

using Ardalis.Result;

using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;

namespace UseCases.UC_Car.Queries;

public class GetCarById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        string TransmissionType,
        string FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        PriceDetail Price,
        LocationDetail Location,
        ManufacturerDetail Manufacturer
    )
    {
        public static async Task<Response> FromEntity(Car car, string masterKey, IAesEncryptionService aesEncryptionService, IKeyManagementService keyManagementService)
        {
            string decryptedKey = keyManagementService.DecryptKey(car.EncryptionKey.EncryptedKey, masterKey);
            string decryptedLicensePlate = await aesEncryptionService.Decrypt(car.EncryptedLicensePlate, decryptedKey, car.EncryptionKey.IV);
            return new(
            car.Id,
            decryptedLicensePlate,
            car.Color,
            car.Seat,
            car.Description,
            car.TransmissionType.ToString(),
            car.FuelType.ToString(),
            car.FuelConsumption,
            car.RequiresCollateral,
            new PriceDetail(car.PricePerHour, car.PricePerDay),
            new LocationDetail(car.Location.X, car.Location.Y),
            new ManufacturerDetail(car.Manufacturer.Id, car.Manufacturer.Name)
             );
        }
    };

    public record PriceDetail(decimal PerHour, decimal PerDay);

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(
        Guid Id,
        string Name
    );

    private sealed class Handler(
        IAppDBContext context,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
            => await context.Cars
                .Include(c => c.Manufacturer)
                .Include(c => c.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
                switch
            {
                null => Result<Response>.NotFound(),
                var car => Result<Response>.Success(await Response.FromEntity(car, encryptionSettings.Key, aesEncryptionService, keyManagementService), "Lấy thông tin xe thành công")
            };
    }
}