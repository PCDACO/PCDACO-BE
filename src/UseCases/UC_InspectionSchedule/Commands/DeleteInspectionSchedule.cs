using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Commands;

public sealed class DeleteInspectionSchedule
{
    public sealed record Command(Guid Id) : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verify current user is consultant
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get the existing schedule
            var schedule = await context
                .InspectionSchedules.Include(s => s.InspectionStatus)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (schedule is null)
                return Result.NotFound(ResponseMessages.InspectionScheduleNotFound);

            // Verify schedule is in pending status
            if (schedule.InspectionStatus.Name.ToLower() != InspectionStatusNames.Pending.ToLower())
                return Result.Error(ResponseMessages.OnlyDeletePendingInspectionSchedule);

            // verify only datetimeoffset.utcnow faster than schedule.InspectionDate 1 day can delete
            if (DateTimeOffset.UtcNow.AddDays(1) > schedule.InspectionDate)
                return Result.Error(
                    ResponseMessages.CannotDeleteScheduleHasInspectionDateLessThen1DayFromNow
                );

            // Soft delete the schedule
            schedule.Delete();

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(ResponseMessages.Deleted);
        }
    }
}
