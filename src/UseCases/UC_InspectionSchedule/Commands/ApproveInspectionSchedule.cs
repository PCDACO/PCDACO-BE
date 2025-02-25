using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Commands;

public sealed class ApproveInspectionSchedule
{
    public sealed record Command(Guid Id, string Note, bool IsApproved)
        : IRequest<Result<Response>>;

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
            var schedule = await context
                .InspectionSchedules.Include(s => s.InspectionStatus)
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

            if (schedule is null)
                return Result.Error(ResponseMessages.InspectionScheduleNotFound);

            // Check if schedule can be updated
            if (!schedule.InspectionStatus.Name.ToLower().Contains("pending"))
                return Result.Error(ResponseMessages.OnlyUpdatePendingInspectionSchedule);

            // Check if the status is existed based on the IsApproved request
            var status = await context.InspectionStatuses.FirstOrDefaultAsync(
                s =>
                    EF.Functions.ILike(s.Name, request.IsApproved ? "%approved%" : "%rejected%")
                    && !s.IsDeleted,
                cancellationToken
            );

            if (status is null && request.IsApproved)
                return Result.Error(ResponseMessages.ApproveStatusNotFound);

            if (status is null && !request.IsApproved)
                return Result.Error(ResponseMessages.RejectStatusNotFound);

            // Update schedule
            schedule.Note = request.Note;
            schedule.InspectionStatusId = status!.Id;
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

            RuleFor(x => x.Note).NotEmpty().WithMessage("Ghi chú không được để trống");

            RuleFor(x => x.IsApproved)
                .NotNull()
                .WithMessage("Trạng thái phê duyệt không được để trống");
        }
    }
}
