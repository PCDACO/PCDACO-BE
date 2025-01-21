using Ardalis.Result;

using Domain.Entities;
using Domain.Enums;
using Domain.Shared;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using UseCases.Abstractions;
using UseCases.DTOs;

using UUIDNext;

namespace UseCases.UC_Car.Commands;

public sealed class CreateCar
{
    public sealed record Query(
        Guid[] AmenityIds,
        Guid ManufacturerId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        TransmissionType TransmissionType,
        FuelType FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal PricePerHour,
        decimal PricePerDay,
        decimal? Latitude,
        decimal? Longtitude
        ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id);

    private sealed class Handler(IAppDBContext context,
        CurrentUser currentUser,
        GeometryFactory geometryFactory,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (currentUser.User!.IsAdmin()) return Result.Error("Bạn không có quyền thực hiện chức năng này !");
            // Check if amenities are exist
            if(request.AmenityIds.Length > 0)
            {
                List<Amenity> amenities = await context.Amenities
                    .AsNoTracking()
                    .Where(a => request.AmenityIds.Contains(a.Id) && !a.IsDeleted)
                    .ToListAsync(cancellationToken);
                if (amenities.Count != request.AmenityIds.Length) return Result.Error("Một số tiện ích không tồn tại !");
            }
            // Check if manufacturer is exist
            Manufacturer? checkingManufacturer = await context.Manufacturers
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.Id == request.ManufacturerId && !m.IsDeleted,
                    cancellationToken);
            if (checkingManufacturer is null) return Result.Error("Hãng xe không tồn tại !");
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedLicensePlate = await aesEncryptionService.Encrypt(request.LicensePlate, key, iv);
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            EncryptionKey newEncryptionKey = new()
            {
                EncryptedKey = encryptedKey,
                IV = iv
            };
            context.EncryptionKeys.Add(newEncryptionKey);
            Guid carId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            Car newCar = new()
            {
                Id = carId,
                ManufacturerId = request.ManufacturerId,
                OwnerId = currentUser.User!.Id,
                EncryptedLicensePlate = encryptedLicensePlate,
                EncryptionKeyId = newEncryptionKey.Id,
                Color = request.Color,
                Seat = request.Seat,
                Description = request.Description,
                TransmissionType = request.TransmissionType,
                FuelType = request.FuelType,
                FuelConsumption = request.FuelConsumption,
                RequiresCollateral = request.RequiresCollateral,
                PricePerHour = request.PricePerHour,
                PricePerDay = request.PricePerDay,
                // Latitude = request.Latitude,
                // Longtitude = request.Longtitude,
                Location = geometryFactory.CreatePoint(new Coordinate((double)request.Longtitude!, (double)request.Latitude!)),
                CarStatistic = new()
                {
                    CarId = carId
                },
                CarAmenities = [.. request.AmenityIds.Select(a => new CarAmenity
                {
                    CarId = carId,
                    AmenityId = a
                })]
            };
            await context.Cars.AddAsync(newCar, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Created(new Response(newCar.Id), "Tạo xe thành công !");
        }
    }

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ManufacturerId)
                .NotEmpty().WithMessage("Phải chọn 1 hãng xe !");
            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("Biển số xe không được để trống !")
                .MinimumLength(8).WithMessage("Biển số xe không được ít hơn 8 kí tự !")
                .MaximumLength(11).WithMessage("Biển số xe không được ít hơn 11 kí tự !");
            RuleFor(x => x.Color)
                .NotEmpty().WithMessage("Màu sắc không được để trống !");
            RuleFor(x => x.Seat)
                .NotEmpty().WithMessage("Số chỗ ngồi không được để trống !")
                .GreaterThan(0).WithMessage("Số chỗ ngồi phải lớn hơn 0 !")
                .LessThan(50).WithMessage("Số chỗ ngồi không được quá 50 !");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả không được quá 500 ký tự !");
            RuleFor(x => x.FuelConsumption)
                .NotEmpty().WithMessage("Mức tiêu hao nhiên liệu không được để trống !")
                .GreaterThan(0).WithMessage("Mức tiêu hao nhiên liệu phải lớn hơn 0 !");
            RuleFor(x => x.PricePerHour)
                .NotEmpty().WithMessage("Giá thuê theo giờ không được để trống !")
                .GreaterThan(0).WithMessage("Giá thuê theo giờ phải lớn hơn 0 !");
            RuleFor(x => x.PricePerDay)
                .NotEmpty().WithMessage("Giá thuê theo ngày không được để trống !")
                .GreaterThan(0).WithMessage("Giá thuê theo ngày phải lớn hơn 0 !");
            RuleFor(x => x.Latitude)
                .NotEmpty().WithMessage("Vĩ độ không được để trống !");
            RuleFor(x => x.Longtitude)
                .NotEmpty().WithMessage("Kinh độ không được để trống !");
        }
    }
}