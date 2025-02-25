using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Car.Commands;

public sealed class CreateCar
{
    public sealed record Command(
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
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Car car) => new(car.Id);
    };

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            // Check if amenities are exist
            if (request.AmenityIds.Length > 0)
            {
                List<Amenity> amenities = await context
                    .Amenities.AsNoTracking()
                    .Where(a => request.AmenityIds.Contains(a.Id))
                    .ToListAsync(cancellationToken);
                if (amenities.Count != request.AmenityIds.Length)
                    return Result.Error(ResponseMessages.AmenitiesNotFound);
            }
            // Check if transmission type is exist
            TransmissionType? checkingTransmissionType = await context
                .TransmissionTypes.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == request.TransmissionTypeId && !t.IsDeleted,
                    cancellationToken
                );
            if (checkingTransmissionType is null)
                return Result.Error(ResponseMessages.TransmissionTypeNotFound);
            // Check if fuel type is exist
            FuelType? checkingFuelType = await context
                .FuelTypes.AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.Id == request.FuelTypeId && !f.IsDeleted,
                    cancellationToken
                );
            if (checkingFuelType is null)
                return Result.Error(ResponseMessages.FuelTypeNotFound);
            // Check if status is exist
            CarStatus? checkingStatus = await context
                .CarStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => EF.Functions.ILike(s.Name, $"%pending%") && !s.IsDeleted,
                    cancellationToken
                );
            if (checkingStatus is null)
                return Result.Error(ResponseMessages.CarStatusNotFound);
            // Check if model is exist
            Model? checkingModel = await context
                .Models.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Id == request.ModelId && !m.IsDeleted,
                    cancellationToken
                );
            if (checkingModel is null)
                return Result.Error(ResponseMessages.ModelNotFound);
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedLicensePlate = await aesEncryptionService.Encrypt(
                request.LicensePlate,
                key,
                iv
            );
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            EncryptionKey newEncryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
            context.EncryptionKeys.Add(newEncryptionKey);
            Guid carId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            Car newCar = new()
            {
                Id = carId,
                ModelId = request.ModelId,
                OwnerId = currentUser.User!.Id,
                EncryptedLicensePlate = encryptedLicensePlate,
                EncryptionKeyId = newEncryptionKey.Id,
                Color = request.Color,
                Seat = request.Seat,
                TransmissionTypeId = request.TransmissionTypeId,
                Description = request.Description,
                FuelTypeId = request.FuelTypeId,
                FuelConsumption = request.FuelConsumption,
                RequiresCollateral = request.RequiresCollateral,
                Price = request.Price,
                StatusId = checkingStatus.Id,
                CarStatistic = new() { CarId = carId },
                CarAmenities =
                [
                    .. request.AmenityIds.Select(a => new CarAmenity
                    {
                        CarId = carId,
                        AmenityId = a,
                    }),
                ],
            };

            CarStatistic newCarStatistic = new() { CarId = carId };

            await context.Cars.AddAsync(newCar, cancellationToken);
            await context.CarStatistics.AddAsync(newCarStatistic, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(
                await Task.FromResult(Response.FromEntity(newCar)),
                ResponseMessages.Created
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
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
