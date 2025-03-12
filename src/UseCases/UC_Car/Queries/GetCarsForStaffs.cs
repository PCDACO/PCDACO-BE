using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Queries;

public class GetCarsForStaffs
{
    public record Query(int PageNumber, int PageSize, string Keyword, string StatusName)
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        Guid ModelId,
        string ModelName,
        Guid OwnerId,
        string OwnerName,
        string LicensePlate,
        string Color,
        int Seat,
        string Status,
        string Description,
        string TransmissionType,
        string FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price,
        LocationDetail? Location,
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
                car.Status.ToString(),
                car.Description,
                car.TransmissionType.ToString() ?? string.Empty,
                car.FuelType.ToString() ?? string.Empty,
                car.FuelConsumption,
                car.RequiresCollateral,
                car.Price,
                car.GPS == null ? null : new LocationDetail(car.GPS.Location.X, car.GPS.Location.Y),
                new ManufacturerDetail(car.Model.Manufacturer.Id, car.Model.Manufacturer.Name),
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
    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url);

    public record AmenityDetail(Guid Id, string Name, string Description);

    public class Handler(
        IAppDBContext context,
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
            IQueryable<Car> gettingQuery = context
                .Cars
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(c => c.Owner).ThenInclude(o => o.Feedbacks)
                .Include(c => c.Model).ThenInclude(o => o.Manufacturer)
                .Include(c => c.EncryptionKey)
                .Include(c => c.ImageCars).ThenInclude(ic => ic.Type)
                .Include(c => c.CarStatistic)
                .Include(c => c.TransmissionType)
                .Include(c => c.FuelType)
                .Include(c => c.GPS)
                .Include(c => c.CarAmenities).ThenInclude(ca => ca.Amenity)
                .Where(c => !c.IsDeleted)
                .Where(c => c.Status == Domain.Enums.CarStatusEnum.Available)
                .OrderByDescending(c => c.Id);
            int count = await gettingQuery.CountAsync(cancellationToken);
            List<Car> cars = await gettingQuery
                .Skip(request.PageSize * (request.PageNumber - 1))
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
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
                    request.PageNumber,
                    request.PageSize
                ),
                ResponseMessages.Fetched
            );
        }
    }
}