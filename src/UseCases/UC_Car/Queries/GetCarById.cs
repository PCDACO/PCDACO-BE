using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Queries;

public class GetCarById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        Guid ModelId,
        string ModelName,
        Guid OwnerId,
        string OwnerName,
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
            IKeyManagementService keyManagementService,
            bool includeContract = false
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
            if (includeContract && car.Contract != null)
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
                car.TransmissionType.Id,
                car.TransmissionType?.Name.ToString() ?? "",
                car.FuelType.Id,
                car.FuelType?.Name.ToString() ?? "",
                car.FuelConsumption,
                car.RequiresCollateral,
                car.Price,
                car.Terms,
                car.Status.ToString(),
                car.CarStatistic.TotalBooking,
                car.CarStatistic.AverageRating,
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
                    .. car.Bookings.Select(b => new BookingSchedule(
                        b.User.Id,
                        b.User.Name,
                        b.StartTime,
                        b.EndTime
                    ))
                ],
                contractDetail
            );
        }
    };

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url, string Type, string Name);

    public record AmenityDetail(Guid Id, string Name, string Description, string Icon);

    public record BookingSchedule(
        Guid DriverId,
        string DriverName,
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

    private sealed class Handler(
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
            Car? gettingCar = await context
                .Cars.Include(c =>
                    c.Bookings.Where(b =>
                        b.StartTime > DateTimeOffset.UtcNow
                        && b.EndTime < DateTimeOffset.UtcNow.AddMonths(3)
                        && b.Status != BookingStatusEnum.Cancelled
                        && b.Status != BookingStatusEnum.Rejected
                        && b.Status != BookingStatusEnum.Expired
                    )
                )
                .ThenInclude(b => b.User)
                .Include(c => c.Owner)
                .ThenInclude(o => o.Feedbacks)
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
                .Where(c => c.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingCar is null)
                return Result.NotFound(ResponseMessages.CarNotFound);

            // Check if user has permission to view contract
            bool canViewContract =
                currentUser.User!.IsAdmin()
                || currentUser.User.IsConsultant()
                || currentUser.User.IsTechnician()
                || gettingCar.OwnerId == currentUser.User.Id;

            return Result<Response>.Success(
                await Response.FromEntity(
                    gettingCar,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService,
                    includeContract: canViewContract
                ),
                ResponseMessages.Fetched
            );
        }
    }
}