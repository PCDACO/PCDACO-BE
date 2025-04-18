using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Commands;

public sealed class InProgressInspectionSchedule
{
    public sealed record Command(Guid Id) : IRequest<Result<Response>>;

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
            // Verify current user is technician
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get the existing schedule
            var schedule = await context.InspectionSchedules.FirstOrDefaultAsync(
                s => s.Id == request.Id && !s.IsDeleted,
                cancellationToken
            );

            if (schedule is null)
                return Result.Error(ResponseMessages.InspectionScheduleNotFound);

            // Check if schedule can be updated
            if (schedule.Status != InspectionScheduleStatusEnum.Pending)
                return Result.Error(ResponseMessages.OnlyUpdatePendingInspectionSchedule);

            // Check if the schedule is assigned to the current user
            if (schedule.TechnicianId != currentUser.User.Id)
                return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");

            // Check if datetimeoffset.utcnow is greater than schedule.InspectionDate above 15 minutes
            if (DateTimeOffset.UtcNow > schedule.InspectionDate.AddMinutes(15))
                return Result.Conflict(ResponseMessages.InspectionScheduleExpired);

            // Check can not inprogress more than 1 schedule for the same technician
            var inProgressSchedule = await context
                .InspectionSchedules.Where(i =>
                    i.TechnicianId == schedule.TechnicianId
                    && i.Status == InspectionScheduleStatusEnum.InProgress
                )
                .FirstOrDefaultAsync(cancellationToken);
            if (inProgressSchedule is not null)
                return Result.Conflict(
                    "Kiểm định viên đã có lịch kiểm định đang diễn ra, không thể thực hiện thêm"
                );

            // Update schedule
            schedule.Status = InspectionScheduleStatusEnum.InProgress;
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
        }
    }
}
