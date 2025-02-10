using Ardalis.Result;
using Domain.Entities;
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
        Guid ModelId,
        Guid TransmissionTypeId,
        Guid FuelTypeId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        string Address,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal PricePerHour,
        decimal PricePerDay,
        decimal? Latitude,
        decimal? Longtitude
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Car car) => new(car.Id);
    };

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        GeometryFactory geometryFactory,
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
            if (currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");
            // Check if amenities are exist
            if (request.AmenityIds.Length > 0)
            {
                List<Amenity> amenities = await context
                    .Amenities.AsNoTracking()
                    .Where(a => request.AmenityIds.Contains(a.Id))
                    .ToListAsync(cancellationToken);
                if (amenities.Count != request.AmenityIds.Length)
                    return Result.Error("Một số tiện nghi không tồn tại !");
            }
            // Check if transmission type is exist
            TransmissionType? checkingTransmissionType = await context
                .TransmissionTypes.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == request.TransmissionTypeId && !t.IsDeleted,
                    cancellationToken
                );
            if (checkingTransmissionType is null)
                return Result.Error("Kiểu truyền động không tồn tại !");
            // Check if fuel type is exist
            FuelType? checkingFuelType = await context
                .FuelTypes.AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.Id == request.FuelTypeId && !f.IsDeleted,
                    cancellationToken
                );
            if (checkingFuelType is null)
                return Result.Error("Kiểu nhiên liệu không tồn tại !");
            // Check if status is exist
            CarStatus? checkingStatus = await context
                .CarStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => EF.Functions.ILike(s.Name, $"%available%") && !s.IsDeleted,
                    cancellationToken
                );
            if (checkingStatus is null)
                return Result.Error("Trạng thái không tồn tại !");
            // Check if model is exist
            Model? checkingModel = await context
                .Models.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Id == request.ModelId && !m.IsDeleted,
                    cancellationToken
                );
            if (checkingModel is null)
                return Result.Error("Mô hình xe không tồn tại !");
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
                Address = request.Address,
                FuelTypeId = request.FuelTypeId,
                FuelConsumption = request.FuelConsumption,
                RequiresCollateral = request.RequiresCollateral,
                PricePerHour = request.PricePerHour,
                PricePerDay = request.PricePerDay,
                StatusId = checkingStatus.Id,
                Location = geometryFactory.CreatePoint(
                    new Coordinate((double)request.Longtitude!, (double)request.Latitude!)
                ),
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
            return Result.Created(
                await Task.FromResult(Response.FromEntity(newCar)),
                "Tạo xe thành công !"
            );
        }
    }

    public sealed class Validator : AbstractValidator<Query>
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
            RuleFor(x => x.Address).NotEmpty().WithMessage("Địa chỉ không được để trống !");
            RuleFor(x => x.FuelConsumption)
                .NotEmpty()
                .WithMessage("Mức tiêu hao nhiên liệu không được để trống !")
                .GreaterThan(0)
                .WithMessage("Mức tiêu hao nhiên liệu phải lớn hơn 0 !");
            RuleFor(x => x.PricePerHour)
                .NotEmpty()
                .WithMessage("Giá thuê theo giờ không được để trống !")
                .GreaterThan(0)
                .WithMessage("Giá thuê theo giờ phải lớn hơn 0 !");
            RuleFor(x => x.PricePerDay)
                .NotEmpty()
                .WithMessage("Giá thuê theo ngày không được để trống !")
                .GreaterThan(0)
                .WithMessage("Giá thuê theo ngày phải lớn hơn 0 !");
            RuleFor(x => x.Latitude).NotEmpty().WithMessage("Vĩ độ không được để trống !");
            RuleFor(x => x.Longtitude).NotEmpty().WithMessage("Kinh độ không được để trống !");
        }
    }
}
