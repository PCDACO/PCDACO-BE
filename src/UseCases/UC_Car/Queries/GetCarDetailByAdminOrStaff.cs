using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Car.Queries;

public class GetCarDetailByAdminOrStaff
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        Guid ModelId,
        string ModelName,
        DateTimeOffset ReleaseDate,
        string Color,
        string LicensePlate,
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
        OwnerDetail Owner,
        StatisticsDetail Statistics,
        LocationDetail? Location,
        PickupLocationDetail PickupLocation,
        ManufacturerDetail Manufacturer,
        ImageDetail[] Images,
        AmenityDetail[] Amenities,
        BookingSchedule[] Bookings,
        ContractDetail? Contract
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

            ContractDetail? contractDetail = null;
            if (car.Contract != null)
            {
                contractDetail = new ContractDetail(
                    car.Contract.Id,
                    car.Contract.Terms,
                    car.Contract.Status.ToString(),
                    car.Contract.OwnerSignatureDate,
                    car.Contract.TechnicianSignatureDate,
                    car.Contract.InspectionResults,
                    car.Contract.GPSDeviceId
                );
            }

            // Calculate total earnings from completed bookings
            decimal totalEarnings = car
                .Bookings.Where(b => b.Status == BookingStatusEnum.Completed)
                .Sum(b => b.TotalAmount);

            // Find the last rented date (most recent completed booking end time)
            DateTimeOffset? lastRented = car
                .Bookings.Where(b => b.Status == BookingStatusEnum.Completed)
                .OrderByDescending(b => b.EndTime)
                .FirstOrDefault()
                ?.EndTime;

            // Get all bookings sorted by date (using UUID creation time) and taking top 4
            var sortedBookings = car
                .Bookings.OrderByDescending(b => GetTimestampFromUuid.Execute(b.Id))
                .Take(4)
                .ToList();

            // decrypt owner phone
            string decryptedOwnerPhone = string.Empty;
            if (car.Owner.EncryptionKey != null && car.Owner.Phone != null)
            {
                string userDecryptedKey = keyManagementService.DecryptKey(
                    car.Owner.EncryptionKey.EncryptedKey,
                    masterKey
                );
                decryptedOwnerPhone = await aesEncryptionService.Decrypt(
                    car.Owner.Phone,
                    userDecryptedKey,
                    car.Owner.EncryptionKey.IV
                );
            }

            var ownerDetail = new OwnerDetail(
                car.Owner.Id,
                car.Owner.Name,
                car.Owner.Email ?? string.Empty,
                decryptedOwnerPhone,
                car.Owner.Address ?? string.Empty,
                car.Owner.AvatarUrl ?? string.Empty
            );

            var statisticsDetail = new StatisticsDetail(
                car.CarStatistic.TotalBooking,
                totalEarnings,
                car.CarStatistic.AverageRating,
                lastRented
            );

            return new(
                car.Id,
                car.Model.Id,
                car.Model.Name,
                car.Model.ReleaseDate,
                car.Color,
                decryptedLicensePlate,
                car.Seat,
                car.Description,
                car.TransmissionType.Id,
                car.TransmissionType?.Name.ToString() ?? "",
                car.FuelType.Id,
                car.FuelType?.Name.ToString() ?? "",
                car.FuelConsumption,
                car.RequiresCollateral,
                car.Price,
                car.Terms,
                car.Status.ToString(),
                ownerDetail,
                statisticsDetail,
                car.GPS == null ? null : new LocationDetail(car.GPS.Location.X, car.GPS.Location.Y),
                new PickupLocationDetail(
                    car.PickupLocation.X,
                    car.PickupLocation.Y,
                    car.PickupAddress
                ),
                new ManufacturerDetail(car.Model.Manufacturer.Id, car.Model.Manufacturer.Name),
                [.. car.ImageCars.Select(i => new ImageDetail(i.Id, i.Url, i.Type.Name, i.Name))],
                [
                    .. car.CarAmenities.Select(a => new AmenityDetail(
                        a.Amenity.Id,
                        a.Amenity.Name,
                        a.Amenity.Description,
                        a.Amenity.IconUrl
                    )),
                ],
                [
                    .. sortedBookings.Select(b => new BookingSchedule(
                        b.Id,
                        b.User.Id,
                        b.User.Name,
                        b.User.AvatarUrl ?? string.Empty,
                        b.StartTime,
                        b.EndTime,
                        b.TotalAmount,
                        b.Status.ToString()
                    )),
                ],
                contractDetail
            );
        }
    };

    public record OwnerDetail(
        Guid Id,
        string Name,
        string Email,
        string Phone,
        string Address,
        string AvatarUrl
    );

    public record StatisticsDetail(
        int TotalBookings,
        decimal TotalEarnings,
        decimal AverageRating,
        DateTimeOffset? LastRented
    );

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url, string Type, string Name);

    public record AmenityDetail(Guid Id, string Name, string Description, string Icon);

    public record BookingSchedule(
        Guid BookingId,
        Guid DriverId,
        string DriverName,
        string AvatarUrl,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        decimal Amount,
        string Status
    );

    public record ContractDetail(
        Guid Id,
        string Terms,
        string Status,
        DateTimeOffset? OwnerSignatureDate,
        DateTimeOffset? TechnicianSignatureDate,
        string? InspectionResults,
        Guid? GPSDeviceId
    );

    public record PickupLocationDetail(double Longitude, double Latitude, string Address);

    internal sealed class Handler(
        IAppDBContext context,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // check if current user is admin or consultant or technician
            if (
                !currentUser.User!.IsAdmin()
                && !currentUser.User.IsConsultant()
                && !currentUser.User.IsTechnician()
            )
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            Car? gettingCar = await context
                .Cars.IgnoreQueryFilters()
                .AsNoTracking()
                .Include(c => c.Bookings)
                .ThenInclude(b => b.User)
                .Include(c => c.Owner)
                .ThenInclude(o => o.Feedbacks)
                .Include(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(c => c.Model)
                .ThenInclude(o => o.Manufacturer)
                .Include(c => c.EncryptionKey)
                .Include(c => c.ImageCars)
                .ThenInclude(ic => ic.Type)
                .Include(c => c.CarStatistic)
                .Include(c => c.TransmissionType)
                .Include(c => c.FuelType)
                .Include(c => c.GPS)
                .Include(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Include(c => c.Contract)
                .Where(c => c.Id == request.Id && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingCar is null)
                return Result.NotFound(ResponseMessages.CarNotFound);

            return Result<Response>.Success(
                await Response.FromEntity(
                    gettingCar,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
