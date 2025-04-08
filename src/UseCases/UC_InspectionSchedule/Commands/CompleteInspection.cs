using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using static Domain.Shared.ContractTemplates.CarContractTemplateGenerator;

namespace UseCases.UC_InspectionSchedule.Commands;

public sealed class CompleteInspection
{
    public sealed record Command(
        Guid ScheduleId,
        string InspectionResults,
        Guid GPSDeviceId,
        bool IsApproved
    ) : IRequest<Result<string>>;

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            // Check if GPS device exists and is available
            var gpsDevice = await context.GPSDevices.FirstOrDefaultAsync(
                d => d.Id == request.GPSDeviceId,
                cancellationToken
            );

            if (gpsDevice == null)
                return Result.NotFound("Không tìm thấy thiết bị GPS");

            if (gpsDevice.Status != DeviceStatusEnum.Available)
                return Result.Error("Thiết bị GPS này đã được sử dụng hoặc không khả dụng");

            var schedule = await context
                .InspectionSchedules.Include(s => s.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(s => s.Car)
                .ThenInclude(c => c.Model)
                .Include(s => s.Photos)
                .Include(s => s.Technician)
                .ThenInclude(u => u.EncryptionKey)
                .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

            if (schedule == null)
                return Result.NotFound("Không tìm thấy lịch kiểm định");

            if (schedule.TechnicianId != currentUser.User.Id)
                return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");

            // Get car contract
            var contract = await context.CarContracts.FirstOrDefaultAsync(
                c => c.CarId == schedule.CarId,
                cancellationToken
            );

            if (contract == null)
                return Result.NotFound("Không tìm thấy hợp đồng xe");

            if (contract.Status != CarContractStatusEnum.OwnerSigned)
                return Result.Error("Hợp đồng không ở trạng thái chờ kiểm định");

            // Update contract with inspection results
            var contractTemplate = new CarContractTemplate
            {
                ContractNumber = contract.Id.ToString(),
                OwnerName = schedule.Car.Owner.Name,
                OwnerLicenseNumber = await DecryptValue(
                    schedule.Car.Owner.EncryptedLicenseNumber,
                    schedule.Car.Owner.EncryptionKey,
                    aesEncryptionService
                ),
                OwnerAddress = schedule.Car.Owner.Address,
                TechnicianName = schedule.Technician.Name,
                TechnicianLicenseNumber = await DecryptValue(
                    schedule.Technician.EncryptedLicenseNumber,
                    schedule.Technician.EncryptionKey,
                    aesEncryptionService
                ),
                CarManufacturer = schedule.Car.Model.Name,
                CarLicensePlate = schedule.Car.LicensePlate,
                CarSeat = schedule.Car.Seat.ToString(),
                CarColor = schedule.Car.Color,
                CarDescription = schedule.Car.Description,
                CarPrice = schedule.Car.Price,
                CarTerms = schedule.Car.Terms,
                InspectionResults = request.InspectionResults,
                InspectionPhotos = schedule.Photos.ToDictionary(p => p.Type, p => p.PhotoUrl),
                GPSDeviceId = request.GPSDeviceId.ToString()
            };

            string contractHtml = GenerateFullContractHtml(contractTemplate);

            if (request.IsApproved)
            {
                // Create CarGPS entry
                var carGPS = new CarGPS
                {
                    CarId = schedule.CarId,
                    DeviceId = request.GPSDeviceId,
                    Location = schedule.Car.PickupLocation // Initial location is car's pickup location
                };

                // Update GPS device status
                gpsDevice.Status = DeviceStatusEnum.InUsed;

                // Update contract
                contract.Terms = contractHtml;
                contract.TechnicianId = currentUser.User.Id;
                contract.TechnicianSignatureDate = DateTimeOffset.UtcNow;
                contract.InspectionResults = request.InspectionResults;
                contract.GPSDeviceId = request.GPSDeviceId;
                contract.Status = CarContractStatusEnum.Completed;

                // Update schedule status
                schedule.Status = InspectionScheduleStatusEnum.Approved;

                // Update car status
                schedule.Car.Status = CarStatusEnum.Available;

                // Save GPS assignment
                await context.CarGPSes.AddAsync(carGPS, cancellationToken);
            }
            else
            {
                // Update contract for rejection
                contract.Terms = contractHtml;
                contract.TechnicianId = currentUser.User.Id;
                contract.TechnicianSignatureDate = DateTimeOffset.UtcNow;
                contract.InspectionResults = request.InspectionResults;
                contract.Status = CarContractStatusEnum.Rejected;

                // Update schedule status
                schedule.Status = InspectionScheduleStatusEnum.Rejected;

                // Update car status
                schedule.Car.Status = CarStatusEnum.Rejected;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                request.IsApproved
                    ? "Kiểm định hoàn tất, xe đã sẵn sàng cho thuê"
                    : "Đã từ chối xe không đạt yêu cầu kiểm định"
            );
        }

        private async Task<string> DecryptValue(
            string encryptedValue,
            EncryptionKey encryptionKey,
            IAesEncryptionService aesEncryptionService
        )
        {
            var decryptedKey = keyManagementService.DecryptKey(
                encryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            return await aesEncryptionService.Decrypt(
                encryptedValue,
                decryptedKey,
                encryptionKey.IV
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ScheduleId)
                .NotEmpty()
                .WithMessage("ID lịch kiểm định không được để trống");

            RuleFor(x => x.InspectionResults)
                .NotEmpty()
                .WithMessage("Kết quả kiểm định không được để trống");

            RuleFor(x => x.GPSDeviceId)
                .NotEmpty()
                .WithMessage("Mã thiết bị GPS không được để trống");
        }
    }
}
