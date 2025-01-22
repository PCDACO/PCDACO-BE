

using Ardalis.Result;

using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Queries;

public class GetCars
{
    public record Query(
        decimal? Latitude,
        decimal? Longtitude,
        decimal? Radius,
        Guid? Manufacturer,
        Guid[]? Amenities,
        Guid? LastCarId,
        int Limit
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        Guid ManufacturerId,
        string ManufacturerName,
        Guid OwnerId,
        string OwnerName,
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
        ManufacturerDetail Manufacturer,
        ImageDetail[] Images,
        AmenityDetail[] Amenities
    )
    {
        public static async Task<Response> FromEntity(Car car, string masterKey, IAesEncryptionService aesEncryptionService, IKeyManagementService keyManagementService)
        {
            string decryptedKey = keyManagementService.DecryptKey(car.EncryptionKey.EncryptedKey, masterKey);
            string decryptedLicensePlate = await aesEncryptionService.Decrypt(car.EncryptedLicensePlate, decryptedKey, car.EncryptionKey.IV);
            return new(
            car.Id,
            car.Manufacturer.Id,
            car.Manufacturer.Name,
            car.Owner.Id,
            car.Owner.Name,
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
            new ManufacturerDetail(car.Manufacturer.Id, car.Manufacturer.Name),
            [.. car.ImageCars.Select(i => new ImageDetail(i.Id, i.Url))],
            [.. car.CarAmenities.Select(a => new AmenityDetail(a.Id, a.Amenity.Name, a.Amenity.Description))]
             );
        }
    };
    public record PriceDetail(decimal PerHour, decimal PerDay);

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(
        Guid Id,
        string Name
    );

    public record ImageDetail(
        Guid Id,
        string Url
    );

    public record AmenityDetail(
        Guid Id,
        string Name,
        string Description
    );

    public class Handler(
        IAppDBContext context,
        GeometryFactory geometryFactory,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Point userLocation = geometryFactory.CreatePoint(new Coordinate((double)request.Longtitude!, (double)request.Latitude!));
            IQueryable<Car> query = context.Cars
                .Include(c => c.Owner)
                .Include(c => c.Manufacturer)
                .Include(c => c.EncryptionKey)
                .Include(c => c.ImageCars)
                .Include(c => c.CarAmenities).ThenInclude(ca => ca.Amenity)
                .Where(c => !c.IsDeleted)
                .Where(c => request.Manufacturer == null || c.ManufacturerId == request.Manufacturer)
                .Where(c => request.Amenities == null || request.Amenities.All(a => c.CarAmenities.Select(ca => ca.AmenityId).Contains(a)))
                .Where(c => ((decimal)c.Location.Distance(userLocation) * 111320) <= (request.Radius ?? 0))
                .OrderByDescending(c => c.Id)
                .Where(c => request.LastCarId == null || c.Id.CompareTo(request.LastCarId) < 0);
            int count = await query.CountAsync(cancellationToken);
            List<Car> cars = await query
                .Take(request.Limit)
                .ToListAsync(cancellationToken);
            return Result.Success(OffsetPaginatedResponse<Response>.Map(
                (await Task.WhenAll(cars.Select(async c => await Response.FromEntity(c, encryptionSettings.Key, aesEncryptionService, keyManagementService)))).AsEnumerable(),
                count,
                request.Limit,
                0
            ));
        }
    }
}