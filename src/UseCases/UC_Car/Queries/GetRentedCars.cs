using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Queries;

public class GetRentedCars
{
    public record Query(int PageNumber, int PageSize, string Keyword)
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
        string Description,
        string TransmissionType,
        string FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price,
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
            return new(
                car.Id,
                car.Model.Id,
                car.Model.Name,
                car.Owner.Id,
                car.Owner.Name,
                car.LicensePlate,
                car.Color,
                car.Seat,
                car.Description,
                car.TransmissionType.Name ?? string.Empty,
                car.FuelType.Name ?? string.Empty,
                car.FuelConsumption,
                car.RequiresCollateral,
                car.Price,
                new LocationDetail(car.GPS.Location.X, car.GPS.Location.Y),
                new ManufacturerDetail(car.Model.Manufacturer.Id, car.Model.Manufacturer.Name),
                [.. car.ImageCars.Select(i => new ImageDetail(i.Id, i.Url, i.Name))],
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

    public record ImageDetail(Guid Id, string Url, string Name);

    public record AmenityDetail(Guid Id, string Name, string Description);

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
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
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            IQueryable<Car> query = context
                .Cars.Include(c => c.Owner)
                .ThenInclude(o => o.Feedbacks)
                .Include(c => c.Model)
                .ThenInclude(m => m.Manufacturer)
                .Include(c => c.ImageCars)
                .Include(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Where(c => c.Status == Domain.Enums.CarStatusEnum.Rented)
                .OrderByDescending(c => c.Owner.Feedbacks.Average(f => f.Point))
                .ThenByDescending(c => c.Id);
            int count = await query.CountAsync(cancellationToken);
            List<Car> cars = await query
                .Skip(request.PageSize * (request.PageNumber - 1))
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();
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
                    request.PageSize,
                    hasNext
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
