using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Contract.Commands;

public sealed class SignCarContract
{
    public record Command(Guid ContractId) : IRequest<Result<Response>>;

    public record Response(Guid ContractId, Guid CarId, string Status)
    {
        public static Response FromEntity(CarContract contract) =>
            new(contract.Id, contract.CarId, contract.Status.ToString());
    };

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check if contract exists and is not deleted
            var contract = await context
                .CarContracts.Include(c => c.Car)
                .FirstOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken);

            if (contract == null)
                return Result.NotFound("Không tìm thấy hợp đồng");

            if (
                contract.Status != CarContractStatusEnum.Pending
                && contract.Status != CarContractStatusEnum.TechnicianSigned
                && contract.Status != CarContractStatusEnum.OwnerSigned
            )
                return Result.Conflict("Hợp đồng không ở trạng thái có thể ký");

            // Check if schedule exists and is in progress
            var schedule = await context.InspectionSchedules.FirstOrDefaultAsync(
                s =>
                    s.CarId == contract.CarId
                    && !s.IsDeleted
                    && s.Status == InspectionScheduleStatusEnum.InProgress,
                cancellationToken
            );

            if (schedule == null)
                return Result.NotFound(
                    "Không tìm thấy lịch kiểm định trong trạng thái đang diễn ra"
                );

            // Owner signing flow
            if (currentUser.User!.IsOwner())
            {
                // Verify that the current user is the car owner
                if (contract.Car.OwnerId != currentUser.User.Id)
                    return Result.Forbidden("Bạn không có quyền ký hợp đồng này");

                // Update contract
                contract.OwnerSignatureDate = DateTimeOffset.UtcNow;
                contract.Status = CarContractStatusEnum.OwnerSigned;

                // Update schedule status to signed
                if (contract.TechnicianSignatureDate != null && contract.OwnerSignatureDate != null)
                    schedule.Status = InspectionScheduleStatusEnum.Signed;

                await context.SaveChangesAsync(cancellationToken);

                return Result.Success(
                    Response.FromEntity(contract),
                    "Hợp đồng đã được chủ xe ký thành công"
                );
            }
            // Technician signing flow
            if (currentUser.User.IsTechnician())
            {
                // Verify that this technician is assigned to the inspection
                if (schedule.TechnicianId != currentUser.User.Id)
                    return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");

                // Update contract
                contract.TechnicianSignatureDate = DateTimeOffset.UtcNow;
                contract.Status = CarContractStatusEnum.TechnicianSigned;

                // Update schedule status to signed
                if (contract.OwnerSignatureDate != null && contract.TechnicianSignatureDate != null)
                    schedule.Status = InspectionScheduleStatusEnum.Signed;

                await context.SaveChangesAsync(cancellationToken);

                return Result.Success(
                    Response.FromEntity(contract),
                    "Hợp đồng đã được kiểm định viên ký thành công"
                );
            }

            // Other roles are not allowed to sign
            return Result.Forbidden("Bạn không có quyền ký hợp đồng này");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractId).NotEmpty().WithMessage("ID hợp đồng không được để trống");
        }
    }
}
