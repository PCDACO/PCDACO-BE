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

namespace UseCases.UC_GPSDevice.Queries;

public class GetGPSDeviceDetail
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string OSBuildId,
        string Name,
        DeviceStatusEnum Status,
        DateTimeOffset CreatedAt,
        CarDetail? CarDetail
    )
    {
        public static async Task<Response> FromEntity(
            GPSDevice device,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            CarDetail? carDetail = null;

            if (device.GPS?.Car != null)
            {
                var car = device.GPS.Car;

                // Get contract detail if available
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
                    .Bookings.Where(b =>
                        b.Status == BookingStatusEnum.Completed
                        || b.Status == BookingStatusEnum.Done
                    )
                    .Sum(b => b.TotalAmount);

                // Find the last rented date (most recent completed booking end time)
                DateTimeOffset? lastRented = car
                    .Bookings.Where(b =>
                        b.Status == BookingStatusEnum.Completed
                        || b.Status == BookingStatusEnum.Done
                    )
                    .OrderByDescending(b => b.EndTime)
                    .FirstOrDefault()
                    ?.EndTime;

                // Decrypt owner phone if available
                string decryptedOwnerPhone = string.Empty;
                if (car.Owner?.EncryptionKey != null && car.Owner.Phone != null)
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
                    car.Owner!.Id,
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

                // Get all future bookings
                var bookings = car
                    .Bookings.Select(b => new BookingSchedule(
                        b.Id,
                        b.User.Id,
                        b.User.Name,
                        b.User.AvatarUrl ?? string.Empty,
                        b.StartTime,
                        b.EndTime
                    ))
                    .ToArray();

                carDetail = new CarDetail(
                    car.Id,
                    car.Model.Id,
                    car.Model.Name,
                    car.Model.ReleaseDate,
                    car.Color,
                    car.LicensePlate,
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
                    device.GPS.Location != null
                        ? new LocationDetail(device.GPS.Location.X, device.GPS.Location.Y)
                        : null,
                    new PickupLocationDetail(
                        car.PickupLocation.X,
                        car.PickupLocation.Y,
                        car.PickupAddress
                    ),
                    new ManufacturerDetail(car.Model.Manufacturer.Id, car.Model.Manufacturer.Name),
                    car.ImageCars.Select(i => new ImageDetail(
                            i.Id,
                            i.Url,
                            i.Type?.Name ?? string.Empty,
                            i.Name
                        ))
                        .ToArray(),
                    car.CarAmenities.Select(a => new AmenityDetail(
                            a.Amenity.Id,
                            a.Amenity.Name,
                            a.Amenity.Description,
                            a.Amenity.IconUrl
                        ))
                        .ToArray(),
                    bookings,
                    contractDetail
                );
            }

            return new(
                device.Id,
                device.OSBuildId,
                device.Name,
                device.Status,
                GetTimestampFromUuid.Execute(device.Id),
                carDetail
            );
        }
    }

    // Record types for the response structure
    public record CarDetail(
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
    );

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
        DateTimeOffset EndTime
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
            // Check authorization
            bool isAuthorized = currentUser.User!.IsAdmin() || currentUser.User!.IsTechnician();

            if (!isAuthorized)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Retrieve GPS device with all related data
            GPSDevice? device = await context
                .GPSDevices.IgnoreQueryFilters()
                .AsNoTracking()
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.Model)
                .ThenInclude(m => m.Manufacturer)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.ImageCars)
                .ThenInclude(ic => ic.Type)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.CarStatistic)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.TransmissionType)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.FuelType)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.Bookings)
                .ThenInclude(b => b.User)
                .Include(d => d.GPS)
                .ThenInclude(g => g.Car)
                .ThenInclude(c => c.Contract)
                .Where(d => !d.IsDeleted)
                .Where(d => d.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (device is null)
                return Result.NotFound(ResponseMessages.GPSDeviceNotFound);

            return Result.Success(
                await Response.FromEntity(
                    device,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
