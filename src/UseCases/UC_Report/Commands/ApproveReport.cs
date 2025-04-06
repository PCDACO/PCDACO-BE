using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Report.Commands;

public class ApproveReport
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
        bool? IsCompensationPaid,
        decimal? CompensationAmount,
        string? CompensationReason
    )
    {
        public static Response FromEntity(BookingReport entity) =>
            new(
                entity.Id,
                entity.Title,
                entity.Description,
                entity.Status.ToString(),
                entity.ResolutionComments,
                entity.ResolvedAt,
                entity.IsCompensationPaid,
                entity.CompensationAmount,
                entity.CompensationReason
            );
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant() && !currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện hành động này");

            var report = await context
                .BookingReports.Include(r => r.CompensationPaidUser)
                .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

            if (report == null)
                return Result.NotFound("Không tìm thấy báo cáo");

            if (request.IsApproved && report.Status != BookingReportStatus.UnderReview)
                return Result.Error("Báo cáo chưa được xem xét");

            report.Status = request.IsApproved
                ? BookingReportStatus.Resolved
                : BookingReportStatus.Rejected;

            report.ResolutionComments = request.Note;
            report.ResolvedAt = DateTimeOffset.UtcNow;
            report.ResolvedById = currentUser.User!.Id;

            // Handle compensation payment status
            if (report.CompensationPaidUserId.HasValue && report.CompensationPaidUser != null)
            {
                if (request.IsApproved)
                {
                    // If approved, mark compensation as paid
                    report.IsCompensationPaid = true;
                    report.CompensationPaidAt = DateTimeOffset.Now;

                    // If the user was banned due to late payment, unban them
                    if (report.CompensationPaidUser.IsBanned)
                    {
                        report.CompensationPaidUser.IsBanned = false;
                        report.CompensationPaidUser.BannedReason = string.Empty;
                    }
                }
                else
                {
                    // If rejected, reset compensation status
                    report.IsCompensationPaid = false;
                    report.CompensationPaidAt = null;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            var message = request.IsApproved
                ? "Báo cáo đã được xử lý thành công"
                : "Báo cáo đã bị từ chối";

            return Result.Success(Response.FromEntity(report), message);
        }
    }
}
