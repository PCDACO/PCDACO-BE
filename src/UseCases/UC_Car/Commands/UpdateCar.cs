using Ardalis.Result;

using Domain.Entities;
using Domain.Shared;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;


using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public sealed class UpdateCar
{
    public sealed record Commamnd(
        Guid CarId,
        Guid[] AmenityIds,
        Guid ModelId,
        Guid TransmissionTypeId,
        Guid FuelTypeId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price
    ) : IRequest<Result>;

    private class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        EncryptionSettings encryptionSettings,
        IKeyManagementService keyManagementService
    ) : IRequestHandler<Commamnd, Result>
    {
        public async Task<Result> Handle(Commamnd request, CancellationToken cancellationToken)
        {
            if (currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");
            Car? checkingCar = await context
                .Cars.Include(c => c.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);
            if (checkingCar is null)
                return Result.Error("Xe không tồn tại");
            List<Amenity> amenities = await context
                .Amenities.AsNoTracking()
                .Where(a => request.AmenityIds.Contains(a.Id))
                .ToListAsync(cancellationToken);
            if (amenities.Count != request.AmenityIds.Length)
                return Result.Error("Một số tiện nghi không tồn tại !");
            TransmissionType? checkingTransmissionType =
                await context.TransmissionTypes.FirstOrDefaultAsync(
                    tt => tt.Id == request.TransmissionTypeId && !tt.IsDeleted,
                    cancellationToken
                );
            if (checkingTransmissionType is null)
                return Result.Error("Kiểu hộp số không tồn tại !");
            // Check if fuel type is exist
            FuelType? checkingFuelType = await context.FuelTypes.FirstOrDefaultAsync(
                ft => ft.Id == request.FuelTypeId && !ft.IsDeleted,
                cancellationToken
            );
            if (checkingFuelType is null)
                return Result.Error("Kiểu nhiên liệu không tồn tại !");
            // Check if model is exist
            Model? checkingModel = await context.Models.FirstOrDefaultAsync(
                m => m.Id == request.ModelId,
                cancellationToken
            );
            if (checkingModel is null)
                return Result.Error("Mô hình xe không tồn tại !");
            // Update car amenities
            await context
                .CarAmenities.Where(ca => ca.CarId == checkingCar.Id)
                .ExecuteDeleteAsync(cancellationToken);
            List<CarAmenity> carAmenities =
            [
                .. amenities.Select(a => new CarAmenity
                {
                    CarId = checkingCar.Id,
                    AmenityId = a.Id,
                }),
            ];
            await context.CarAmenities.AddRangeAsync(carAmenities, cancellationToken);
            string decryptedKey = keyManagementService.DecryptKey(
                checkingCar.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );
            string encryptedLicensePlate = await aesEncryptionService.Encrypt(
                request.LicensePlate,
                decryptedKey,
                checkingCar.EncryptionKey.IV
            );
            // Update car
            checkingCar.ModelId = request.ModelId;
            checkingCar.EncryptedLicensePlate = encryptedLicensePlate;
            checkingCar.Color = request.Color;
            checkingCar.Seat = request.Seat;
            checkingCar.Description = request.Description;
            checkingCar.TransmissionTypeId = request.TransmissionTypeId;
            checkingCar.FuelTypeId = request.FuelTypeId;
            checkingCar.FuelConsumption = request.FuelConsumption;
            checkingCar.RequiresCollateral = request.RequiresCollateral;
            checkingCar.Price = request.Price;
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
            RuleFor(x => x.ModelId).NotEmpty().WithMessage("Phải chọn 1 mô hình xe !");
            RuleFor(x => x.LicensePlate)
                .NotEmpty()
                .WithMessage("Biển số xe không được để trống !")
                .MinimumLength(8)
                .WithMessage("Biển số xe không được ít hơn 8 kí tự !")
                .MaximumLength(11)
                .WithMessage("Biển số xe không được ít hơn 11 kí tự !");
            RuleFor(x => x.Color).NotEmpty().WithMessage("Màu sắc không được để trống !");
            RuleFor(x => x.Seat)
                .NotEmpty()
                .WithMessage("Số chỗ ngồi không được để trống !")
                .GreaterThan(0)
                .WithMessage("Số chỗ ngồi phải lớn hơn 0 !")
                .LessThan(50)
                .WithMessage("Số chỗ ngồi không được quá 50 !");
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Mô tả không được quá 500 ký tự !");
            RuleFor(x => x.FuelConsumption)
                .NotEmpty()
                .WithMessage("Mức tiêu hao nhiên liệu không được để trống !")
                .GreaterThan(0)
                .WithMessage("Mức tiêu hao nhiên liệu phải lớn hơn 0 !");
            RuleFor(x => x.Price)
                .NotEmpty()
                .WithMessage("Giá thuê theo ngày không được để trống !")
                .GreaterThan(0)
                .WithMessage("Giá thuê theo ngày phải lớn hơn 0 !");
        }
    }
}
