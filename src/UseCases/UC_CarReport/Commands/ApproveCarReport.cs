using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CarReport.Commands;

public sealed class ApproveCarReport
{
    public sealed record Command(Guid ReportId, bool IsApproved, string Note)
        : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        string Title,
        string Description,
        string Status,
        string? ResolutionComments,
        DateTimeOffset? ResolvedAt,
        InspectionScheduleDetail? InspectionScheduleDetail
    )
    {
        public static Response FromEntity(CarReport entity)
        {
            return new(
                entity.Id,
                entity.Title,
                entity.Description,
                entity.Status.ToString(),
                entity.ResolutionComments,
                entity.ResolvedAt,
                entity.InspectionSchedule != null
                    ? new InspectionScheduleDetail(
                        entity.InspectionSchedule.Id,
                        entity.InspectionSchedule.TechnicianId,
                        entity.InspectionSchedule.Technician?.Name,
                        entity.InspectionSchedule.Technician?.AvatarUrl,
                        entity.InspectionSchedule.Status,
                        entity.InspectionSchedule.InspectionAddress,
                        entity.InspectionSchedule.InspectionDate,
                        entity.InspectionSchedule.Note,
                        [.. entity.InspectionSchedule.Photos.Select(p => p.PhotoUrl)]
                    )
                    : null
            );
        }
    }

    public sealed record InspectionScheduleDetail(
        Guid? Id,
        Guid? TechnicianId,
        string? TechnicianName,
        string? TechnicianAvatar,
        InspectionScheduleStatusEnum? Status,
        string? InspectionAddress,
        DateTimeOffset? InspectionDate,
        string? Note,
        string[]? PhotoUrls
    );

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant() && !currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            var report = await context
                .CarReports.Include(r => r.InspectionSchedule)
                .ThenInclude(i => i!.Technician)
                .Include(r => r.Car)
                .Include(r => r.InspectionSchedule!.Photos)
                .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

            if (report == null)
                return Result.NotFound(ResponseMessages.ReportNotFound);

            if (request.IsApproved && report.Status != CarReportStatus.UnderReview)
                return Result.Error(ResponseMessages.ReportNotUnderReviewed);

            if (
                (
                    report.InspectionSchedule == null
                    || report.InspectionSchedule.Status != InspectionScheduleStatusEnum.Approved
                ) && request.IsApproved
            )
                return Result.Error("Lịch kiểm tra chưa được kỹ thuật viên duyệt");

            report.Status = request.IsApproved
                ? CarReportStatus.Resolved
                : CarReportStatus.Rejected;

            report.ResolutionComments = request.Note;
            report.ResolvedAt = DateTimeOffset.UtcNow;
            report.ResolvedById = currentUser.User!.Id;

            // If approved
            if (request.IsApproved)
            {
                if (report.ReportType == CarReportType.DeactivateCar)
                {
                    report.Car.Status = CarStatusEnum.Inactive;
                    report.Car.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    report.Car.Status = CarStatusEnum.Available;
                    report.Car.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            var message = request.IsApproved
                ? "Báo cáo xe đã được xử lý thành công"
                : "Báo cáo xe đã bị từ chối";

            return Result.Success(Response.FromEntity(report), message);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReportId).NotEmpty().WithMessage("ID báo cáo không được để trống");

            RuleFor(x => x.Note)
                .NotEmpty()
                .WithMessage("Ghi chú không được để trống")
                .MaximumLength(1000)
                .WithMessage("Ghi chú không được quá 1000 ký tự");
        }
    }
}
