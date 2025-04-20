using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
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
        Guid? ManufacturerId,
        Guid[]? Amenities,
        Guid? FuelTypes,
        Guid? TransmissionTypes,
        Guid? LastCarId,
        int Limit,
        string Keyword,
        DateTimeOffset? StartTime,
        DateTimeOffset? EndTime
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        Guid ModelId,
        string ModelName,
        Guid OwnerId,
        string OwnerName,
        string OwnerAvatarUrl,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        Guid TransmissionId,
        string TransmissionType,
        Guid FuelTypeId,
        string FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price,
        string Terms,
        string Status,
        int TotalRented,
        decimal AverageRating,
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
            return new(
                Id: car.Id,
                ModelId: car.Model.Id,
                ModelName: car.Model.Name,
                OwnerId: car.Owner.Id,
                OwnerName: car.Owner.Name,
                OwnerAvatarUrl: car.Owner.AvatarUrl,
                LicensePlate: car.LicensePlate,
                Color: car.Color,
                Seat: car.Seat,
                Description: car.Description,
                TransmissionId: car.TransmissionType.Id,
                TransmissionType: car.TransmissionType.Name ?? string.Empty,
                FuelTypeId: car.FuelType.Id,
                FuelType: car.FuelType.Name ?? string.Empty,
                FuelConsumption: car.FuelConsumption,
                RequiresCollateral: car.RequiresCollateral,
                Price: car.Price,
                Terms: car.Terms,
                Status: car.Status.ToString(),
                TotalRented: car.CarStatistic.TotalBooking,
                AverageRating: car.CarStatistic.AverageRating,
                Location: car.GPS == null
                    ? null
                    : new LocationDetail(car.GPS.Location.X, car.GPS.Location.Y),
                Manufacturer: new ManufacturerDetail(
                    car.Model.Manufacturer.Id,
                    car.Model.Manufacturer.Name
                ),
                Images:
                [
                    .. car.ImageCars?.Select(i => new ImageDetail(i.Id, i.Url, i.Type.Name, i.Name))
                        ?? []
                ],
                Amenities:
                [
                    .. car.CarAmenities.Select(a => new AmenityDetail(
                        a.Amenity.Id,
                        a.Amenity.Name,
                        a.Amenity.Description,
                        a.Amenity.IconUrl
                    )),
                ]
            );
        }
    };

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url, string Type, string Name);

    public record AmenityDetail(Guid Id, string Name, string Description, string Icon);

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
            IQueryable<Car> gettingCarQuery = context
                .Cars.AsNoTracking()
                .Include(c => c.Owner)
                .ThenInclude(o => o.Feedbacks)
                .Include(c => c.Model)
                .ThenInclude(o => o.Manufacturer)
                .Include(c =>
                    c.Bookings.Where(b =>
                        b.Status != BookingStatusEnum.Cancelled
                        && b.Status != BookingStatusEnum.Rejected
                        && b.Status != BookingStatusEnum.Expired
                    )
                )
                .Include(c => c.ImageCars)
                .ThenInclude(ic => ic.Type)
                .Include(c => c.CarStatistic)
                .Include(c => c.TransmissionType)
                .Include(c => c.FuelType)
                .Include(c => c.GPS)
                .Include(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Where(c => !c.IsDeleted)
                .Where(c => c.Status == Domain.Enums.CarStatusEnum.Available);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                string language = "simple";
                var searchTerm = request.Keyword;

                // Split search terms into words to handle partial matches better
                var searchWords = searchTerm
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Replace("-", "").Replace(" ", ""))
                    .ToList();

                // Apply EF-supported full-text search
                gettingCarQuery = gettingCarQuery.Where(c =>
                    EF.Functions.ToTsVector(
                            language,
                            (c.Model.Name ?? "")
                                + " "
                                + (c.Description ?? "")
                                + " "
                                + (c.Color ?? "")
                                + " "
                                + (c.Model.Manufacturer.Name ?? "")
                        )
                        .Matches(EF.Functions.PlainToTsQuery(language, searchTerm))
                    ||
                    // Special handling for license plate with different formats
                    (
                        c.LicensePlate != null
                        && searchWords.Any(word =>
                            c.LicensePlate.Replace("-", "").Replace(" ", "").Contains(word)
                        )
                    )
                    ||
                    // Handle numeric searches - using string contains for more flexible matching
                    (
                        c.Price.ToString().Contains(request.Keyword)
                        || c.Seat.ToString() == request.Keyword
                        || c.FuelConsumption.ToString().Contains(request.Keyword)
                    )
                );
            }

            // Filter by manufacturer
            if (request.ManufacturerId != null)
            {
                gettingCarQuery = gettingCarQuery.Where(c =>
                    c.Model.ManufacturerId == request.ManufacturerId
                );
            }

            // Filter by amenities
            if (request.Amenities != null && request.Amenities.Length > 0)
            {
                gettingCarQuery = gettingCarQuery.Where(c =>
                    request.Amenities.All(a =>
                        c.CarAmenities.Select(ca => ca.AmenityId).Contains(a)
                    )
                );
            }

            // Filter by fuel types
            if (request.FuelTypes != null)
            {
                gettingCarQuery = gettingCarQuery.Where(c => c.FuelTypeId == request.FuelTypes);
            }

            // Filter by transmission types
            if (request.TransmissionTypes != null)
            {
                gettingCarQuery = gettingCarQuery.Where(c =>
                    c.TransmissionTypeId == request.TransmissionTypes
                );
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue)
            {
                var startDate = request.StartTime.Value.Date;
                var endDate = request.EndTime.Value.Date;

                gettingCarQuery = gettingCarQuery.Where(c =>
                    !c.Bookings.Any(b => b.StartTime.Date <= endDate && b.EndTime.Date >= startDate)
                );
            }
            if (request.Longtitude is not null && request.Latitude is not null)
            {
                Point userLocation = geometryFactory.CreatePoint(
                    new Coordinate((double)request.Longtitude!, (double)request.Latitude!)
                );
                gettingCarQuery = gettingCarQuery.Where(c =>
                    ((decimal)c.GPS.Location.Distance(userLocation) * 111320)
                    <= (request.Radius ?? 0)
                );
            }
            gettingCarQuery = gettingCarQuery
                .Where(c => request.LastCarId == null || request.LastCarId > c.Id)
                .OrderByDescending(c => c.Owner.Feedbacks.Average(f => f.Point))
                .ThenByDescending(c => c.Id);
            int count = await gettingCarQuery.CountAsync(cancellationToken);
            List<Car> carResult = await gettingCarQuery
                .Take(request.Limit)
                .ToListAsync(cancellationToken);
            var processedCars = carResult
                .Select(car =>
                {
                    if (request.StartTime.HasValue && request.EndTime.HasValue)
                    {
                        var hasConflict = car.Bookings.Any(b =>
                            IsDateConflict(
                                request.StartTime.Value,
                                request.EndTime.Value,
                                b.StartTime,
                                b.EndTime
                            )
                        );

                        if (hasConflict)
                        {
                            car.Status = Domain.Enums.CarStatusEnum.Rented;
                        }
                    }
                    return car;
                })
                .ToList();
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    (
                        await Task.WhenAll(
                            processedCars.Select(async c =>
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

        private static bool IsDateConflict(
            DateTimeOffset requestStart,
            DateTimeOffset requestEnd,
            DateTimeOffset bookingStart,
            DateTimeOffset bookingEnd
        )
        {
            var requestStartDate = requestStart.Date;
            var requestEndDate = requestEnd.Date;
            var bookingStartDate = bookingStart.Date;
            var bookingEndDate = bookingEnd.Date;

            return (requestStartDate <= bookingEndDate.AddDays(1))
                && (requestEndDate >= bookingStartDate.AddDays(-1));
        }
    }
}
