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

public class GetCarById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        Guid ModelId,
        string ModelName,
        Guid OwnerId,
        string OwnerName,
        string OwnerPhoneNumber,
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
        ContractDetail? Contract,
        FeedbackDetail[] Feedbacks
    )
    {
        public static async Task<Response> FromEntityAsync(
            Car car,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService,
            string masterKey,
            bool includeContract = false
        )
        {
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

            // Get all driver feedbacks for this car's bookings
            var feedbacks = car
                .Bookings.SelectMany(b => b.Feedbacks)
                .Where(f => f.Type == FeedbackTypeEnum.ToOwner)
                .Select(f => new FeedbackDetail(
                    f.Id,
                    f.UserId,
                    f.User.Name,
                    f.User.AvatarUrl,
                    f.Point,
                    f.Content,
                    GetTimestampFromUuid.Execute(f.Id)
                ))
                .OrderByDescending(f => f.CreatedAt)
                .ToArray();

            // Decrypt owner's phone
            string decryptedOwnerPhone = string.Empty;
            if (car.Owner.EncryptionKey != null)
            {
                string ownerDecryptedKey = keyManagementService.DecryptKey(
                    car.Owner.EncryptionKey.EncryptedKey,
                    masterKey
                );

                decryptedOwnerPhone = await aesEncryptionService.Decrypt(
                    car.Owner.Phone,
                    ownerDecryptedKey,
                    car.Owner.EncryptionKey.IV
                );
            }

            // Process bookings and decrypt driver phones
            var bookingSchedules = new List<BookingSchedule>();
            foreach (var booking in car.Bookings)
            {
                string decryptedDriverPhone = string.Empty;
                if (booking.User?.EncryptionKey != null)
                {
                    string driverDecryptedKey = keyManagementService.DecryptKey(
                        booking.User.EncryptionKey.EncryptedKey,
                        masterKey
                    );

                    decryptedDriverPhone = await aesEncryptionService.Decrypt(
                        booking.User.Phone,
                        driverDecryptedKey,
                        booking.User.EncryptionKey.IV
                    );
                }

                if (
                    booking.Status != BookingStatusEnum.Approved
                    && booking.Status != BookingStatusEnum.ReadyForPickup
                    && booking.Status != BookingStatusEnum.Ongoing
                    && booking.Status != BookingStatusEnum.Completed
                    && booking.Status != BookingStatusEnum.Done
                )
                    continue;

                bookingSchedules.Add(
                    new BookingSchedule(
                        booking.Id,
                        booking.User!.Id,
                        booking.User.Name,
                        decryptedDriverPhone,
                        booking.User.AvatarUrl,
                        booking.StartTime,
                        booking.ActualReturnTime,
                        booking.Status.ToString()
                    )
                );
            }

            return new(
                car.Id,
                car.Model.Id,
                car.Model.Name,
                car.Owner.Id,
                car.Owner.Name,
                decryptedOwnerPhone,
                car.LicensePlate,
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
                [.. bookingSchedules.OrderByDescending(b => b.BookingId)],
                contractDetail,
                feedbacks
            );
        }
    };

    public record LocationDetail(double Longtitude, double Latitude);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url, string Type, string Name);

    public record AmenityDetail(Guid Id, string Name, string Description, string Icon);

    public record BookingSchedule(
        Guid BookingId,
        Guid DriverId,
        string DriverName,
        string DriverPhone,
        string AvatarUrl,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
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

    public record FeedbackDetail(
        Guid Id,
        Guid UserId,
        string UserName,
        string UserAvatar,
        int Rating,
        string Content,
        DateTimeOffset CreatedAt
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
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
                .ThenInclude(u => u.EncryptionKey)
                .Include(c => c.Bookings)
                .ThenInclude(b => b.Feedbacks)
                .ThenInclude(f => f.User)
                .Include(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(c => c.Owner)
                .ThenInclude(o => o.Feedbacks)
                .Include(c => c.Model)
                .ThenInclude(o => o.Manufacturer)
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
                await Response.FromEntityAsync(
                    car: gettingCar,
                    aesEncryptionService: aesEncryptionService,
                    keyManagementService: keyManagementService,
                    masterKey: encryptionSettings.Key,
                    includeContract: canViewContract
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
