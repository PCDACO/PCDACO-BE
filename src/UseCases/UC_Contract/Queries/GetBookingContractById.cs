using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Contract.Queries;

public sealed class GetBookingContractById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        string Terms,
        Guid BookingId,
        CarDetail Car,
        UserDetail Owner,
        UserDetail Driver,
        string Status,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        DateTimeOffset? DriverSignatureDate,
        DateTimeOffset? OwnerSignatureDate,
        decimal BasePrice,
        decimal TotalAmount,
        DateTimeOffset CreatedAt
    );

    public record CarDetail(
        Guid Id,
        string ModelName,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        LocationDetail? Location,
        ManufacturerDetail Manufacturer,
        ImageDetail[] ImageCarDetail,
        AmenityDetail[] Amenities
    );

    public record LocationDetail(double Longtitude, double Latitude);

    public record UserDetail(Guid Id, string Name, string Email, string Address, string Phone);

    public record ManufacturerDetail(Guid Id, string Name);

    public record ImageDetail(Guid Id, string Url, string Type, string Name);

    public record AmenityDetail(Guid Id, string Name, string Description, string IconUrl);

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
            // Get contract with all related data
            var contract = await context
                .Contracts.AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Model)
                .ThenInclude(c => c.Manufacturer)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.ImageCars)
                .ThenInclude(i => i.Type)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.GPS)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.CarAmenities)
                .ThenInclude(a => a.Amenity)
                .Include(c => c.Booking)
                .ThenInclude(b => b.User)
                .ThenInclude(u => u.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contract == null)
                return Result.NotFound("Không tìm thấy hợp đồng");

            // check user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền xem thông tin này");

            // Decrypt phone
            var (driverPhone, ownerPhone) = await DecryptedUserPhones(
                contract,
                encryptionSettings.Key
            );

            // Create response
            var response = new Response(
                Id: contract.Id,
                Terms: contract.Terms,
                BookingId: contract.BookingId,
                Car: new CarDetail(
                    Id: contract.Booking.Car.Id,
                    ModelName: contract.Booking.Car.Model.Name,
                    LicensePlate: contract.Booking.Car.LicensePlate,
                    Color: contract.Booking.Car.Color,
                    Seat: contract.Booking.Car.Seat,
                    Description: contract.Booking.Car.Description,
                    Location: contract.Booking.Car.GPS != null
                        ? new LocationDetail(
                            Longtitude: contract.Booking.Car.GPS.Location.X,
                            Latitude: contract.Booking.Car.GPS.Location.Y
                        )
                        : null,
                    Manufacturer: new ManufacturerDetail(
                        Id: contract.Booking.Car.Model.Manufacturer.Id,
                        Name: contract.Booking.Car.Model.Manufacturer.Name
                    ),
                    ImageCarDetail:
                    [
                        .. contract.Booking.Car.ImageCars.Select(i => new ImageDetail(
                            Id: i.Id,
                            Url: i.Url,
                            Type: i.Type.Name,
                            Name: i.Name
                        )),
                    ],
                    Amenities:
                    [
                        .. contract.Booking.Car.CarAmenities.Select(a => new AmenityDetail(
                            Id: a.Amenity.Id,
                            Name: a.Amenity.Name,
                            Description: a.Amenity.Description,
                            IconUrl: a.Amenity.IconUrl
                        )),
                    ]
                ),
                Owner: new UserDetail(
                    Id: contract.Booking.Car.Owner.Id,
                    Name: contract.Booking.Car.Owner.Name,
                    Email: contract.Booking.Car.Owner.Email,
                    Address: contract.Booking.Car.Owner.Address,
                    Phone: ownerPhone
                ),
                Driver: new UserDetail(
                    Id: contract.Booking.User.Id,
                    Name: contract.Booking.User.Name,
                    Email: contract.Booking.User.Email,
                    Address: contract.Booking.User.Address,
                    Phone: driverPhone
                ),
                Status: contract.Status.ToString(),
                StartDate: contract.StartDate,
                EndDate: contract.EndDate,
                DriverSignatureDate: contract.DriverSignatureDate,
                OwnerSignatureDate: contract.OwnerSignatureDate,
                BasePrice: contract.Booking.BasePrice,
                TotalAmount: contract.Booking.TotalAmount,
                CreatedAt: GetTimestampFromUuid.Execute(contract.Id)
            );

            return Result.Success(response);
        }

        private async Task<(string, string)> DecryptedUserPhones(
            Contract contract,
            string masterKey
        )
        {
            // Driver
            string driverKey = keyManagementService.DecryptKey(
                contract.Booking.User.EncryptionKey.EncryptedKey,
                masterKey
            );

            string decryptedDriverPhone = await aesEncryptionService.Decrypt(
                contract.Booking.User.Phone,
                driverKey,
                contract.Booking.User.EncryptionKey.IV
            );

            // Owner
            string ownerKey = keyManagementService.DecryptKey(
                contract.Booking.Car.Owner.EncryptionKey.EncryptedKey,
                masterKey
            );

            string decryptedOwnerPhone = await aesEncryptionService.Decrypt(
                contract.Booking.Car.Owner.Phone,
                ownerKey,
                contract.Booking.Car.Owner.EncryptionKey.IV
            );

            return (decryptedDriverPhone, decryptedOwnerPhone);
        }
    }
}
