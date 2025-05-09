using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Commands;

public sealed class UpdateInspectionSchedule
{
    public sealed record Command(
        Guid Id,
        Guid TechnicianId,
        string InspectionAddress,
        DateTimeOffset InspectionDate
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(InspectionSchedule schedule) => new(schedule.Id);
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Verify current user is consultant
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get the existing schedule
            var schedule = await context
                .InspectionSchedules.IgnoreQueryFilters()
                .AsSplitQuery()
                .Include(i => i.Car)
                .ThenInclude(c => c.Contract)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (schedule is null)
                return Result.Error(ResponseMessages.InspectionScheduleNotFound);

            // Check if schedule can be updated (only pending or inprogress schedules can be updated)
            if (
                schedule.Status != Domain.Enums.InspectionScheduleStatusEnum.Pending
                && schedule.Status != Domain.Enums.InspectionScheduleStatusEnum.InProgress
            )
                return Result.Conflict(
                    ResponseMessages.OnlyUpdatePendingOrInprogressInspectionSchedule
                );

            // Verify technician exists and is a technician
            var technician = await context
                .Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.Id == request.TechnicianId && !u.IsDeleted,
                    cancellationToken
                );

            if (technician is null || !technician.IsTechnician())
                return Result.Error(ResponseMessages.TechnicianNotFound);

            // Get technician's existing inspection schedules that are not expired or rejected
            var technicianSchedules = await context
                .InspectionSchedules.AsNoTracking()
                .Where(s => s.TechnicianId == request.TechnicianId)
                .Where(s => s.Id != request.Id) // Exclude the current schedule being updated
                .Where(s => !s.IsDeleted)
                .Where(s =>
                    s.Status != Domain.Enums.InspectionScheduleStatusEnum.Expired
                    && s.Status != Domain.Enums.InspectionScheduleStatusEnum.Rejected
                )
                .Select(s => new { s.InspectionDate, s.Status })
                .ToListAsync(cancellationToken);

            var requestedTime = request.InspectionDate;

            // check conflicts with pending or in progress schedules
            var hasActiveScheduleConflict = technicianSchedules.Any(schedule =>
                schedule.Status != Domain.Enums.InspectionScheduleStatusEnum.Approved
                && Math.Abs((schedule.InspectionDate - requestedTime).TotalMinutes) < 60
            );

            if (hasActiveScheduleConflict)
                return Result.Error(ResponseMessages.TechnicianHasInspectionScheduleWithinOneHour);

            // Reset signature in the associated car contract if technician is changed
            if (
                schedule.TechnicianId != request.TechnicianId
                && schedule.Type == Domain.Enums.InspectionScheduleType.NewCar
            )
            {
                var contract = schedule.Car?.Contract;
                if (contract != null)
                {
                    contract.OwnerSignature = null;
                    contract.OwnerSignatureDate = null;
                    contract.TechnicianSignature = null;
                    contract.TechnicianSignatureDate = null;
                    contract.Status = Domain.Enums.CarContractStatusEnum.Pending;
                    contract.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            // Update schedule
            schedule.TechnicianId = request.TechnicianId;
            schedule.InspectionAddress = request.InspectionAddress;
            schedule.InspectionDate = request.InspectionDate;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(schedule), ResponseMessages.Updated);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id lịch kiểm định không được để trống");

            RuleFor(x => x.TechnicianId)
                .NotEmpty()
                .WithMessage("Id kiểm định viên không được để trống");

            RuleFor(x => x.InspectionAddress)
                .NotEmpty()
                .WithMessage("Địa chỉ kiểm định không được để trống");

            RuleFor(x => x.InspectionDate)
                .NotEmpty()
                .WithMessage("Ngày kiểm định không được để trống")
                .Must(date => date >= DateTimeOffset.UtcNow)
                .WithMessage("Thời điểm kiểm định phải lớn hơn hoặc bằng thời điểm hiện tại");
        }
    }
}
