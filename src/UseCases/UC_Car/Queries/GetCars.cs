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
        Guid? Model,
        Guid[]? Amenities,
        Guid? FuelTypes,
        Guid? TransmissionTypes,
        Guid? LastCarId,
        int Limit
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        Guid ModelId,
        string ModelName,
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
        public static async Task<Response> FromEntity(
            Car car,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                car.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedLicensePlate = await aesEncryptionService.Decrypt(
                car.EncryptedLicensePlate,
                decryptedKey,
                car.EncryptionKey.IV
            );
            return new(
                car.Id,
                car.Model.Id,
                car.Model.Name,
                car.Owner.Id,
                car.Owner.Name,
                decryptedLicensePlate,
                car.Color,
                car.Seat,
                car.Description,
                car.TransmissionType.ToString() ?? string.Empty,
                car.FuelType.ToString() ?? string.Empty,
                car.FuelConsumption,
                car.RequiresCollateral,
                new PriceDetail(car.PricePerHour, car.PricePerDay),
                new LocationDetail(car.Location.X, car.Location.Y),
                new ManufacturerDetail(car.Model.Id, car.Model.Name),
                [.. car.ImageCars.Select(i => new ImageDetail(i.Id, i.Url))],
                [
                    .. car.CarAmenities.Select(a => new AmenityDetail(
                        a.Id,
                        a.Amenity.Name,
                        a.Amenity.Description
                    )),
                ]
            );
        }
    };

    public record PriceDetail(decimal PerHour, decimal PerDay);

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url);

    public record AmenityDetail(Guid Id, string Name, string Description);

    public class Handler(
        IAppDBContext context,
        GeometryFactory geometryFactory,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            Point userLocation = geometryFactory.CreatePoint(
                new Coordinate((double)request.Longtitude!, (double)request.Latitude!)
            );
            IQueryable<Car> query = context
                .Cars.Include(c => c.Owner)
                .ThenInclude(o => o.Feedbacks)
                .Include(c => c.Model)
                .ThenInclude(o => o.Manufacturer)
                .Include(c => c.EncryptionKey)
                .Include(c => c.ImageCars)
                .Include(c => c.CarStatus)
                .Include(c => c.TransmissionType)
                .Include(c => c.FuelType)
                .Include(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Where(c => EF.Functions.ILike(c.CarStatus.Name, $"%available%"))
                .Where(c => request.Model == null || c.ModelId == request.Model)
                .Where(c =>
                    request.Amenities == null
                    || request.Amenities.All(a =>
                        c.CarAmenities.Select(ca => ca.AmenityId).Contains(a)
                    )
                )
                .Where(c => request.FuelTypes == null || c.FuelTypeId == request.FuelTypes)
                .Where(c =>
                    request.TransmissionTypes == null
                    || c.TransmissionTypeId == request.TransmissionTypes
                )
                .Where(c =>
                    ((decimal)c.Location.Distance(userLocation) * 111320) <= (request.Radius ?? 0)
                )
                .OrderByDescending(c => c.Owner.Feedbacks.Average(f => f.Point))
                .ThenByDescending(c => c.Id)
                .Where(c => request.LastCarId == null || c.Id.CompareTo(request.LastCarId) < 0);
            int count = await query.CountAsync(cancellationToken);
            List<Car> cars = await query.Take(request.Limit).ToListAsync(cancellationToken);
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    (
                        await Task.WhenAll(
                            cars.Select(async c =>
                                await Response.FromEntity(
                                    c,
                                    encryptionSettings.Key,
                                    aesEncryptionService,
                                    keyManagementService
                                )
                            )
                        )
                    ).AsEnumerable(),
                    count,
                    request.Limit,
                    0
                )
            );
        }
    }
}
