using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Report.Commands;

public class ReviewReport
{
    public sealed record Command(Guid ReportId) : IRequest<Result<Response>>;

    public sealed record Response(Guid ReportId, BookingReportStatus Status)
    {
        public static Response FromEntity(BookingReport entity) => new(entity.Id, entity.Status);
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden("Bạn không có quyền thực hiện hành động này");

            var report = await context.BookingReports.FirstOrDefaultAsync(
                r => r.Id == request.ReportId,
                cancellationToken
            );

            if (report == null)
                return Result.NotFound("Không tìm thấy báo cáo");

            if (report.Status != BookingReportStatus.Pending)
                return Result.Error("Báo cáo đã được xử lý");

            report.Status = BookingReportStatus.UnderReview;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(report), "Báo cáo đang được xem xét");
        }
    }
}
