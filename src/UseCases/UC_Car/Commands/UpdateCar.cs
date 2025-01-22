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

namespace UseCases.UC_Car.Commands;

public sealed class UpdateCar
{
    public sealed record Commamnd(
        Guid CarId,
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
    ) : IRequest<Result>;

    private class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        GeometryFactory geometryFactory,
        IAesEncryptionService aesEncryptionService,
        EncryptionSettings encryptionSettings,
        IKeyManagementService keyManagementService
    ) : IRequestHandler<Commamnd, Result>
    {
        public async Task<Result> Handle(Commamnd request, CancellationToken cancellationToken)
        {
            if (currentUser.User!.IsAdmin()) return Result.Error("Bạn không có quyền thực hiện chức năng này !");
            Car? checkingCar = await context.Cars
                .Include(c => c.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.CarId && !c.IsDeleted, cancellationToken);
            if (checkingCar is null) return Result.Error("Xe không tồn tại");
            List<Amenity> amenities = await context.Amenities
                .AsNoTracking()
                .Where(a => request.AmenityIds.Contains(a.Id) && !a.IsDeleted)
                .ToListAsync(cancellationToken);
            if (amenities.Count != request.AmenityIds.Length) return Result.Error("Một số tiện ích không tồn tại !");
            // Check if manufacturer is exist
            Manufacturer? checkingManufacturer = await context.Manufacturers.FirstOrDefaultAsync(m =>
                m.Id == request.ManufacturerId && !m.IsDeleted,
                cancellationToken);
            if (checkingManufacturer is null) return Result.Error("Hãng xe không tồn tại !");
            // Update car amenities
            await context.CarAmenities.Where(ca => ca.CarId == checkingCar.Id)
                .ExecuteDeleteAsync(cancellationToken);
            List<CarAmenity> carAmenities = [.. amenities.Select(a => new CarAmenity
            {
                CarId = checkingCar.Id,
                AmenityId = a.Id
            })];
            await context.CarAmenities.AddRangeAsync(carAmenities, cancellationToken);
            string decryptedKey = keyManagementService.DecryptKey(checkingCar.EncryptionKey.EncryptedKey, encryptionSettings.Key);
            string encryptedLicensePlate = await aesEncryptionService.Encrypt(request.LicensePlate,
                                                                            decryptedKey,
                                                                            checkingCar.EncryptionKey.IV);
            // Update car
            checkingCar.ManufacturerId = request.ManufacturerId;
            checkingCar.EncryptedLicensePlate = encryptedLicensePlate;
            checkingCar.Color = request.Color;
            checkingCar.Seat = request.Seat;
            checkingCar.Description = request.Description;
            checkingCar.TransmissionType = request.TransmissionType;
            checkingCar.FuelType = request.FuelType;
            checkingCar.FuelConsumption = request.FuelConsumption;
            checkingCar.RequiresCollateral = request.RequiresCollateral;
            checkingCar.PricePerHour = request.PricePerHour;
            checkingCar.PricePerDay = request.PricePerDay;
            checkingCar.Location = geometryFactory.CreatePoint(new Coordinate((double)request.Longtitude!, (double)request.Latitude!));
            checkingCar.UpdatedAt = DateTimeOffset.UtcNow;
            // Save changes
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Cập nhật xe thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Commamnd>
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