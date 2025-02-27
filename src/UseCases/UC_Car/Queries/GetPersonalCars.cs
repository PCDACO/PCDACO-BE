using Ardalis.Result;

using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Queries;

public class GetPersonalCars
{
    public record Query(
        Guid? Model,
        Guid[]? Amenities,
        Guid? FuelTypes,
        Guid? TransmissionTypes,
        Guid? LastCarId,
        int Limit,
        string? StatusName = CarStatusNames.Available
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
                car.Description,
                car.TransmissionType.Name ?? string.Empty,
                car.FuelType.Name ?? string.Empty,
                car.FuelConsumption,
                car.RequiresCollateral,
                car.Price,
                car.GPS == null ? null : new LocationDetail(car.GPS.Location.X, car.GPS.Location.Y),
                new ManufacturerDetail(car.Model.Manufacturer.Id, car.Model.Manufacturer.Name),
                [.. car.ImageCars?.Select(i => new ImageDetail(i.Id, i.Url)) ?? []],
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
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            IQueryable<Car> gettingCarQuery = context
                .Cars
                .AsNoTracking()
                .Include(c => c.Owner).ThenInclude(o => o.Feedbacks)
                .Include(c => c.Model).ThenInclude(o => o.Manufacturer)
                .Include(c => c.EncryptionKey)
                .Include(c => c.ImageCars)
                .Include(c => c.CarStatus)
                .Include(c => c.TransmissionType)
                .Include(c => c.FuelType)
                .Include(c => c.GPS)
                .Include(c => c.CarAmenities).ThenInclude(ca => ca.Amenity)
                .Where(c => !c.IsDeleted)
                .Where(c => EF.Functions.ILike(c.CarStatus.Name, $"%{request.StatusName}%"))
                .Where(c => c.OwnerId === currentUser.User!.Id)
                .Where(c => request.Model == null || c.ModelId == request.Model)
                .Where(c =>
                    request.Amenities == null || request.Amenities.Length == 0
                    || request.Amenities.All(a =>
                        c.CarAmenities.Select(ca => ca.AmenityId).Contains(a)
                    )
                )
                .Where(c => request.FuelTypes == null || c.FuelTypeId == request.FuelTypes)
                .Where(c =>
                    request.TransmissionTypes == null
                    || c.TransmissionTypeId == request.TransmissionTypes
                );
            gettingCarQuery = gettingCarQuery
                .Where(c => request.LastCarId == null || request.LastCarId > c.Id)
                .OrderByDescending(c => c.Owner.Feedbacks.Average(f => f.Point))
                .ThenByDescending(c => c.Id);
            int count = await gettingCarQuery.CountAsync(cancellationToken);
            List<Car> carResult = await gettingCarQuery.ToListAsync(cancellationToken);
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    (
                        await Task.WhenAll(
                            carResult.Select(async c =>
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
                    0,
                    0
                ),
                ResponseMessages.Fetched
            );
        }
    }
}