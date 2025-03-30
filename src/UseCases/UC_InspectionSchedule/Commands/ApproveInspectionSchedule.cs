using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
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
            var schedule = await context.InspectionSchedules.FirstOrDefaultAsync(
                s => s.Id == request.Id && !s.IsDeleted,
                cancellationToken
            );

            if (schedule is null)
                return Result.NotFound(ResponseMessages.InspectionScheduleNotFound);

            if (schedule.TechnicianId != currentUser.User.Id)
                return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");

            // Check if schedule can be updated
            if (schedule.Status != Domain.Enums.InspectionScheduleStatusEnum.Signed)
                return Result.Error(ResponseMessages.OnlyUpdateInSignedInspectionSchedule);

            // Verify only datetimeoffset.utcnow faster than schedule.InspectionDate 1 hour above can not update
            if (DateTimeOffset.UtcNow > schedule.InspectionDate.AddHours(1))
                return Result.Error(ResponseMessages.InspectionScheduleExpired);

            if (request.IsApproved)
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
                    TechnicianLicenseNumber = await DecryptValue(
                        schedule.Technician.EncryptedLicenseNumber,
                        schedule.Technician.EncryptionKey,
                        aesEncryptionService
                    ),
                    CarManufacturer = schedule.Car.Model.Name,
                    CarLicensePlate = await DecryptValue(
                        schedule.Car.EncryptedLicensePlate,
                        schedule.Car.EncryptionKey,
                        aesEncryptionService
                    ),
                    CarSeat = schedule.Car.Seat.ToString(),
                    CarColor = schedule.Car.Color,
                    CarDescription = schedule.Car.Description,
                    CarPrice = schedule.Car.Price,
                    CarTerms = schedule.Car.Terms,
                    InspectionResults = request.IsApproved ? "Đã duyệt" : "Không duyệt",
                    InspectionPhotos = schedule.Photos.ToDictionary(p => p.Type, p => p.PhotoUrl),
                    GPSDeviceId = contract.GPSDeviceId.ToString()!,
                };

                string contractHtml = GenerateFullContractHtml(contractTemplate);

                //update contract
                contract.Terms = contractHtml;
                contract.InspectionResults = request.IsApproved ? "Đã duyệt" : "Không duyệt";
                contract.Status = Domain.Enums.CarContractStatusEnum.Completed;
                contract.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Update schedule
            schedule.Note = request.Note;
            schedule.Status = request.IsApproved
                ? Domain.Enums.InspectionScheduleStatusEnum.Approved
                : Domain.Enums.InspectionScheduleStatusEnum.Rejected;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;
            // Set Car Status into available
            if (request.IsApproved)
            {
                await context
                    .Cars.Where(c => !c.IsDeleted)
                    .Where(c => c.Id == schedule.CarId)
                    .ExecuteUpdateAsync(
                        c => c.SetProperty(c => c.Status, Domain.Enums.CarStatusEnum.Available),
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
