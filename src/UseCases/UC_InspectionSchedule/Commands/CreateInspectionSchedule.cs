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

public sealed class CreateInspectionSchedule
{
    public sealed record Command(
        Guid TechnicianId,
        Guid CarId,
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

            // Verify car exists and is in pending status
            var car = await context.Cars.FirstOrDefaultAsync(
                c => c.Id == request.CarId && !c.IsDeleted,
                cancellationToken
            );

            if (car is null)
                return Result.Error(ResponseMessages.CarNotFound);

            if (car.Status != CarStatusEnum.Pending)
                return Result.Error(ResponseMessages.CarIsNotInPending);

            // Verify user exists and is a technician
            var technician = await context
                .Users.Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.Id == request.TechnicianId && !u.IsDeleted,
                    cancellationToken
                );

            if (technician is null || !technician.IsTechnician())
                return Result.Error(ResponseMessages.TechnicianNotFound);

            // Check for existing active schedules for the car
            var existingActiveSchedule = await context
                .InspectionSchedules.AsNoTracking()
                .Where(s => s.CarId == request.CarId)
                .Where(s => !s.IsDeleted)
                .Where(s =>
                    s.Status != InspectionScheduleStatusEnum.Expired
                    && s.Status != InspectionScheduleStatusEnum.Rejected
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (existingActiveSchedule != null)
                return Result.Error(ResponseMessages.CarHadInspectionSchedule);

            // Check for expired schedules with the same technician
            var existingExpiredSchedule = await context
                .InspectionSchedules.AsNoTracking()
                .Where(s => s.CarId == request.CarId)
                .Where(s => !s.IsDeleted)
                .Where(s => s.Status == InspectionScheduleStatusEnum.Expired)
                .Where(s => s.TechnicianId == request.TechnicianId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingExpiredSchedule != null)
                return Result.Error(
                    ResponseMessages.CarHadExpiredInspectionScheduleWithThisTechnician
                );

            // Get technician's existing inspection schedules that are not expired or rejected
            var technicianSchedules = await context
                .InspectionSchedules.AsNoTracking()
                .Where(s => s.TechnicianId == request.TechnicianId)
                .Where(s => !s.IsDeleted)
                .Where(s =>
                    s.Status != InspectionScheduleStatusEnum.Expired
                    && s.Status != InspectionScheduleStatusEnum.Rejected
                )
                .Select(s => new { s.InspectionDate, s.Status })
                .ToListAsync(cancellationToken);

            var requestedTime = request.InspectionDate;
            // check conflicts with approved schedules
            var hasApprovedScheduleConflict = technicianSchedules.Any(schedule =>
                schedule.Status == InspectionScheduleStatusEnum.Approved
                && requestedTime <= schedule.InspectionDate
            );

            if (hasApprovedScheduleConflict)
                return Result.Error(ResponseMessages.HasOverLapScheduleWithTheSameTechnician);

            // check conflicts with pending or in progress schedules
            var hasActiveScheduleConflict = technicianSchedules.Any(schedule =>
                schedule.Status != InspectionScheduleStatusEnum.Approved
                && Math.Abs((schedule.InspectionDate - requestedTime).TotalMinutes) < 60
            );

            if (hasActiveScheduleConflict)
                return Result.Error(ResponseMessages.TechnicianHasInspectionScheduleWithinOneHour);

            // Create inspection schedule
            var schedule = new InspectionSchedule
            {
                TechnicianId = request.TechnicianId,
                CarId = request.CarId,
                InspectionAddress = request.InspectionAddress,
                InspectionDate = request.InspectionDate,
                CreatedBy = currentUser.User.Id,
            };

            await context.InspectionSchedules.AddAsync(schedule, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(schedule), ResponseMessages.Created);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TechnicianId)
                .NotEmpty()
                .WithMessage("id kiểm định viên không được để trống");

            RuleFor(x => x.CarId).NotEmpty().WithMessage("id xe không được để trống");

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
