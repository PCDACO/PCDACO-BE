using Ardalis.Result;
using Domain.Constants;
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

public sealed class ApproveInspectionSchedule
{
    public sealed record Command(Guid Id, string Note, bool IsApproved)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(InspectionSchedule schedule) => new(schedule.Id);
    }

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
            // Verify current user is technician
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get the existing schedule
            var schedule = await context
                .InspectionSchedules.IgnoreQueryFilters()
                .AsSplitQuery()
                .Include(s => s.Car)
                .ThenInclude(s => s.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(s => s.Car)
                .ThenInclude(s => s.Model)
                .Include(s => s.Car)
                .ThenInclude(s => s.GPS)
                .Include(s => s.Car)
                .ThenInclude(s => s.Contract)
                .Include(s => s.CarReport)
                .Include(s => s.Technician)
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (schedule is null)
                return Result.NotFound(ResponseMessages.InspectionScheduleNotFound);

            if (schedule.TechnicianId != currentUser.User.Id)
                return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");

            // Check if schedule can be approved or rejected
            if (request.IsApproved)
            {
                // For approval, schedule must be InProgress or Signed
                if (
                    schedule.Status != InspectionScheduleStatusEnum.Signed
                    && schedule.Status != InspectionScheduleStatusEnum.InProgress
                )
                {
                    return Result.Error(
                        "Chỉ có thể phê duyệt lịch kiểm định ở trạng thái đã ký hoặc đang xử lý"
                    );
                }
            }
            else
            {
                // For rejection, schedule must be Pending, InProgress or Signed
                if (
                    schedule.Status != InspectionScheduleStatusEnum.Signed
                    && schedule.Status != InspectionScheduleStatusEnum.InProgress
                    && schedule.Status != InspectionScheduleStatusEnum.Pending
                )
                {
                    return Result.Error(
                        "Chỉ có thể từ chối lịch kiểm định ở trạng thái chờ xử lý, đã ký hoặc đang xử lý"
                    );
                }
            }

            bool isDeactivationReport =
                schedule.CarReportId != null
                && schedule.CarReport != null
                && schedule.CarReport.ReportType == CarReportType.DeactivateCar;

            // Check if car is not attached to any gps when approving then return error
            // Exception: this schedule belongs to a deactivation report
            if (schedule.Car.GPS is null && request.IsApproved && !isDeactivationReport)
                return Result.Error("Xe chưa được gán thiết bị gps không thể duyệt lịch kiểm định");

            if (schedule.Car.GPS != null && request.IsApproved && isDeactivationReport)
                return Result.Error(
                    "Vui lòng gỡ thiết bị gps trước khi duyệt lịch cho báo cáo rút xe khỏi hệ thống"
                );

            // Verify only datetimeoffset.utcnow faster than schedule.InspectionDate 1 hour above can not update
            if (DateTimeOffset.UtcNow > schedule.InspectionDate.AddHours(1))
                return Result.Error(ResponseMessages.InspectionScheduleExpired);

            if (request.IsApproved && schedule.Type == InspectionScheduleType.NewCar)
            {
                // Get car contract
                var contract = await context.CarContracts.FirstOrDefaultAsync(
                    c => c.CarId == schedule.CarId && !c.IsDeleted,
                    cancellationToken
                );

                if (contract == null)
                    return Result.NotFound("Không tìm thấy hợp đồng xe");

                if (contract.OwnerSignatureDate == null)
                    return Result.Error("Hợp đồng chưa được ký bởi chủ xe");

                if (contract.TechnicianSignatureDate == null)
                    return Result.Error("Hợp đồng chưa được ký bởi kiểm định viên");

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
                    CarManufacturer = schedule.Car.Model.Name,
                    CarLicensePlate = schedule.Car.LicensePlate,
                    CarSeat = schedule.Car.Seat.ToString(),
                    CarColor = schedule.Car.Color,
                    CarDescription = schedule.Car.Description,
                    CarPrice = schedule.Car.Price,
                    CarTerms = schedule.Car.Terms,
                    InspectionResults = request.IsApproved ? "Đã duyệt" : "Không duyệt",
                    InspectionPhotos = [],
                    GPSDeviceId = contract.GPSDeviceId.ToString()!,
                    OwnerSignatureImageUrl = contract.OwnerSignature!,
                    TechnicianSignatureImageUrl = contract.TechnicianSignature!,
                };

                string contractHtml = GenerateFullContractHtml(contractTemplate);

                //update contract
                contract.Terms = contractHtml;
                contract.InspectionResults = request.IsApproved ? "Đã duyệt" : "Không duyệt";
                contract.Status = CarContractStatusEnum.Completed;
                contract.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Update schedule
            schedule.Note = request.Note;
            schedule.Status = request.IsApproved
                ? InspectionScheduleStatusEnum.Approved
                : InspectionScheduleStatusEnum.Rejected;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            // reset contract signature if schedule is not approved
            if (!request.IsApproved && schedule.Type == InspectionScheduleType.NewCar)
            {
                var contract = schedule.Car?.Contract;
                if (contract != null)
                {
                    contract.OwnerSignature = null;
                    contract.OwnerSignatureDate = null;
                    contract.TechnicianSignature = null;
                    contract.TechnicianSignatureDate = null;
                    contract.Status = CarContractStatusEnum.Pending;
                    contract.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }
            // Set Car Status into available
            if (request.IsApproved)
            {
                if (isDeactivationReport)
                {
                    await context
                        .Cars.Where(c => !c.IsDeleted)
                        .Where(c => c.Id == schedule.CarId)
                        .ExecuteUpdateAsync(
                            c => c.SetProperty(c => c.Status, CarStatusEnum.Inactive),
                            cancellationToken: cancellationToken
                        );
                }
                else
                {
                    await context
                        .Cars.Where(c => !c.IsDeleted)
                        .Where(c => c.Id == schedule.CarId)
                        .ExecuteUpdateAsync(
                            c => c.SetProperty(c => c.Status, CarStatusEnum.Available),
                            cancellationToken: cancellationToken
                        );
                }
            }
            if (!request.IsApproved && schedule.Type != InspectionScheduleType.NewCar)
            {
                await context
                    .Cars.Where(c => !c.IsDeleted)
                    .Where(c => c.Id == schedule.CarId)
                    .ExecuteUpdateAsync(
                        c => c.SetProperty(c => c.Status, CarStatusEnum.Available),
                        cancellationToken: cancellationToken
                    );
            }
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(Response.FromEntity(schedule), ResponseMessages.Updated);
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
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id lịch kiểm định không được để trống");

            RuleFor(x => x.IsApproved)
                .NotNull()
                .WithMessage("Trạng thái phê duyệt không được để trống");
        }
    }
}
