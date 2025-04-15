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
        DateTimeOffset InspectionDate,
        Guid? ReportId = null,
        InspectionScheduleType Type = InspectionScheduleType.NewCar
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
            var car = await context
                .Cars.IgnoreQueryFilters()
                .Include(c => c.GPS)
                .FirstOrDefaultAsync(c => c.Id == request.CarId && !c.IsDeleted, cancellationToken);

            if (car is null)
                return Result.Error(ResponseMessages.CarNotFound);

            if (request.Type == InspectionScheduleType.Incident)
            {
                // Check if the car is in pending or rented status
                if (car.Status == CarStatusEnum.Pending || car.Status == CarStatusEnum.Rented)
                    return Result.Error(
                        "Không thể tao lịch sự cố cho xe đang chờ duyệt hoặc đã được thuê"
                    );
                // Check only car doesn't have any active booking can continue
                var activeBooking = await context
                    .Bookings.Where(b => b.CarId == request.CarId)
                    .Where(b =>
                        b.Status == BookingStatusEnum.Pending
                        || b.Status == BookingStatusEnum.Ongoing
                        || b.Status == BookingStatusEnum.ReadyForPickup
                        || b.Status == BookingStatusEnum.Approved
                    )
                    .FirstOrDefaultAsync(cancellationToken);
                if (activeBooking != null)
                    return Result.Error("Xe đang có lịch đặt không thể tạo lịch cho sự cố");

                // update car status to maintain
                car.Status = CarStatusEnum.Maintain;
                car.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (request.Type == InspectionScheduleType.ChangeGPS)
            {
                // Check cargps of car is null
                if (car.GPS == null)
                    return Result.Error(
                        "Xe chưa được gán thiết bị gps không thể tạo lịch đổi thiết bị gps"
                    );

                // Check only car doesn't have any active booking can continue
                var activeBooking = await context
                    .Bookings.Where(b => b.CarId == request.CarId)
                    .Where(b =>
                        b.Status == BookingStatusEnum.Pending
                        || b.Status == BookingStatusEnum.Ongoing
                        || b.Status == BookingStatusEnum.ReadyForPickup
                        || b.Status == BookingStatusEnum.Approved
                    )
                    .FirstOrDefaultAsync(cancellationToken);
                if (activeBooking != null)
                    return Result.Error("Xe đang có lịch đặt không thể tạo lịch đổi thiết bị gps");

                // update car status to inactive
                car.Status = CarStatusEnum.Inactive;
                car.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (car.Status != CarStatusEnum.Pending)
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

            // Check if the report exists and is not deleted and is under review
            if (request.ReportId != null && request.Type == InspectionScheduleType.Incident)
            {
                var report = await context.BookingReports.FirstOrDefaultAsync(
                    r => r.Id == request.ReportId && !r.IsDeleted,
                    cancellationToken
                );
                if (report is null)
                    return Result.Error(ResponseMessages.ReportNotFound);
                if (report.Status != BookingReportStatus.UnderReview)
                    return Result.Error(ResponseMessages.ReportNotUnderReviewed);

                report.Status = BookingReportStatus.UnderReview;
                report.ResolvedById = currentUser.User.Id;
            }

            var existingActiveSchedule = await context
                .InspectionSchedules.AsNoTracking()
                .Where(s => s.CarId == request.CarId)
                .Where(s => !s.IsDeleted)
                .Where(s =>
                    s.Status != InspectionScheduleStatusEnum.Expired
                    && s.Status != InspectionScheduleStatusEnum.Rejected
                    && s.Status != InspectionScheduleStatusEnum.Approved
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (existingActiveSchedule != null)
                return Result.Error(ResponseMessages.CarHadInspectionSchedule);

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
                ReportId = request.ReportId,
                InspectionAddress = request.InspectionAddress,
                InspectionDate = request.InspectionDate,
                CreatedBy = currentUser.User.Id,
                Type = request.Type,
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
