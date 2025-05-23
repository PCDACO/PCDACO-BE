using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Contract.Commands;

public sealed class UpdateContract
{
    public sealed record Command(Guid ScheduleId) : IRequest<Result<Response>>;

    public sealed record Response(Guid ContractId, Guid CarId)
    {
        public static Response FromEntity(CarContract contract) => new(contract.Id, contract.CarId);
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Verify current user is technician
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Find the inspection schedule
            var schedule = await context
                .InspectionSchedules.Include(s => s.Car)
                .ThenInclude(c => c.GPS)
                .FirstOrDefaultAsync(
                    s => s.Id == request.ScheduleId && !s.IsDeleted,
                    cancellationToken
                );

            if (schedule is null)
                return Result.NotFound("Không tìm thấy lịch kiểm định");

            if (schedule.Status != InspectionScheduleStatusEnum.InProgress)
                return Result.Conflict("Lịch kiểm định không ở trạng thái đang diễn ra");

            if (schedule.TechnicianId != currentUser.User.Id)
                return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");

            if (schedule.Car.GPS == null)
                return Result.Error("Xe chưa được gán thiết bị GPS");

            // Find or create contract
            var contract = await context.CarContracts.FirstOrDefaultAsync(
                c => c.CarId == schedule.CarId && !c.IsDeleted,
                cancellationToken
            );

            if (contract is null)
            {
                // Create a new contract if not found
                contract = new CarContract
                {
                    CarId = schedule.CarId,
                    Status = CarContractStatusEnum.Pending,
                };
                await context.CarContracts.AddAsync(contract, cancellationToken);
            }
            else
            {
                //Check if device existed in any contract
                var existingContract = await context
                    .CarContracts.Where(c =>
                        c.GPSDeviceId == schedule.Car.GPS.DeviceId && c.Id != contract.Id
                    )
                    .FirstOrDefaultAsync(cancellationToken);
                if (existingContract != null)
                {
                    return Result.Error(
                        "Thiết bị GPS này đã được gán cho hợp đồng khác, hãy gỡ thiết bị GPS trước"
                    );
                }
                // Update existing contract
                contract.Status = CarContractStatusEnum.Pending;
            }

            // Update the contract
            contract.TechnicianId = currentUser.User.Id;
            contract.GPSDeviceId = schedule.Car.GPS.DeviceId;
            contract.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(contract), "Cập nhật hợp đồng thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ScheduleId)
                .NotEmpty()
                .WithMessage("ID lịch kiểm định không được để trống");
        }
    }
}
