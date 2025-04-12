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
using UseCases.Utils;

namespace UseCases.UC_Contract.Queries;

public sealed class GetContractDetail
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record CarDetail(
        Guid Id,
        string ModelName,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        string Terms,
        string Status,
        string TransmissionType,
        string FuelType,
        decimal Price,
        bool RequiresCollateral,
        decimal FuelConsumption,
        ImageDetail[] ImageCarDetail,
        ManufacturerDetail ManufacturerDetail,
        LocationDetail? Location,
        AmenityDetail[] Amenities
    );

    public sealed record ManufacturerDetail(Guid Id, string Name);

    public sealed record ImageDetail(Guid Id, string Url, string Type, string Name);

    public sealed record LocationDetail(double Longtitude, double Latitude);

    public sealed record AmenityDetail(Guid Id, string Name, string Description, string IconUrl);

    public sealed record OwnerDetail(
        Guid Id,
        string Name,
        string Email,
        string Phone,
        string Address,
        string? AvatarUrl
    );

    public sealed record TechnicianDetail(Guid Id, string Name, string Email, string Phone);

    public sealed record Response(
        Guid Id,
        string Status,
        DateTimeOffset? OwnerSignatureDate,
        DateTimeOffset? TechnicianSignatureDate,
        string? InspectionResults,
        Guid? GpsDeviceId,
        DateTimeOffset CreatedAt,
        CarDetail Car,
        OwnerDetail Owner,
        TechnicianDetail? Technician
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
            // Check if user has permission to view contracts
            if (
                !(
                    currentUser.User!.IsAdmin()
                    || currentUser.User.IsConsultant()
                    || currentUser.User.IsTechnician()
                )
            )
            {
                return Result.Forbidden("Bạn không có quyền xem hợp đồng này");
            }

            // Fetch contract with related data
            var contract = await context
                .CarContracts.IgnoreQueryFilters()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Car)
                .ThenInclude(car => car.Model)
                .ThenInclude(m => m.Manufacturer)
                .Include(c => c.Car)
                .ThenInclude(car => car.TransmissionType)
                .Include(c => c.Car)
                .ThenInclude(car => car.FuelType)
                .Include(c => c.Car)
                .ThenInclude(car => car.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(c => c.Car)
                .ThenInclude(c => c.ImageCars)
                .Include(c => c.Car)
                .ThenInclude(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Include(c => c.Car)
                .ThenInclude(c => c.GPS)
                .Include(c => c.Technician)
                .ThenInclude(t => t!.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (contract == null)
            {
                return Result.NotFound("Không tìm thấy hợp đồng");
            }

            // Decrypt owner's phone if available
            string ownerPhone = "";
            if (contract.Car.Owner.Phone != null)
            {
                var ownerKey = keyManagementService.DecryptKey(
                    contract.Car.Owner.EncryptionKey.EncryptedKey,
                    encryptionSettings.Key
                );

                ownerPhone = await aesEncryptionService.Decrypt(
                    contract.Car.Owner.Phone,
                    ownerKey,
                    contract.Car.Owner.EncryptionKey.IV
                );
            }

            // Decrypt technician's phone if available
            string technicianPhone = "";
            if (contract.Technician?.Phone != null)
            {
                var technicianKey = keyManagementService.DecryptKey(
                    contract.Technician.EncryptionKey.EncryptedKey,
                    encryptionSettings.Key
                );

                technicianPhone = await aesEncryptionService.Decrypt(
                    contract.Technician.Phone,
                    technicianKey,
                    contract.Technician.EncryptionKey.IV
                );
            }

            // Create response
            var response = new Response(
                Id: contract.Id,
                Status: contract.Status.ToString(),
                OwnerSignatureDate: contract.OwnerSignatureDate,
                TechnicianSignatureDate: contract.TechnicianSignatureDate,
                InspectionResults: contract.InspectionResults,
                GpsDeviceId: contract.GPSDeviceId,
                CreatedAt: GetTimestampFromUuid.Execute(contract.Id),
                Car: new CarDetail(
                    Id: contract.Car.Id,
                    ModelName: contract.Car.Model.Name,
                    LicensePlate: contract.Car.LicensePlate,
                    Color: contract.Car.Color,
                    Seat: contract.Car.Seat,
                    Description: contract.Car.Description,
                    Terms: contract.Car.Terms,
                    Status: contract.Car.Status.ToString(),
                    TransmissionType: contract.Car.TransmissionType.Name,
                    FuelType: contract.Car.FuelType.Name,
                    Price: contract.Car.Price,
                    RequiresCollateral: contract.Car.RequiresCollateral,
                    FuelConsumption: contract.Car.FuelConsumption,
                    ImageCarDetail:
                    [
                        .. contract.Car.ImageCars.Select(i => new ImageDetail(
                            Id: i.Id,
                            Url: i.Url,
                            Type: i.Type.Name.ToString(),
                            Name: i.Name
                        )),
                    ],
                    ManufacturerDetail: new ManufacturerDetail(
                        Id: contract.Car.Model.Manufacturer.Id,
                        Name: contract.Car.Model.Manufacturer.Name
                    ),
                    Location: contract.Car.GPS == null
                        ? null
                        : new LocationDetail(
                            Longtitude: contract.Car.GPS.Location.X,
                            Latitude: contract.Car.GPS.Location.Y
                        ),
                    Amenities:
                    [
                        .. contract.Car.CarAmenities.Select(a => new AmenityDetail(
                            Id: a.Amenity.Id,
                            Name: a.Amenity.Name,
                            Description: a.Amenity.Description,
                            IconUrl: a.Amenity.IconUrl
                        )),
                    ]
                ),
                Owner: new OwnerDetail(
                    Id: contract.Car.Owner.Id,
                    Name: contract.Car.Owner.Name,
                    Email: contract.Car.Owner.Email,
                    Phone: ownerPhone,
                    Address: contract.Car.Owner.Address ?? "",
                    AvatarUrl: contract.Car.Owner.AvatarUrl
                ),
                Technician: contract.Technician != null
                    ? new TechnicianDetail(
                        Id: contract.Technician.Id,
                        Name: contract.Technician.Name,
                        Email: contract.Technician.Email,
                        Phone: technicianPhone
                    )
                    : null
            );

            return Result.Success(response);
        }
    }
}
