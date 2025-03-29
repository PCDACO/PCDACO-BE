using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Report.Queries;

public class GetUnderReviewReports
{
    public sealed record Query : IRequest<Result<List<Response>>>;

    public sealed record Response(
        Guid Id,
        Guid BookingId,
        string Title,
        BookingReportType ReportType,
        string Description,
        BookingReportStatus Status,
        DateTimeOffset CreatedAt,
        string ReportedByName
    )
    {
        public static Response FromEntity(BookingReport entity) =>
            new(
                entity.Id,
                entity.BookingId,
                entity.Title,
                entity.ReportType,
                entity.Description,
                entity.Status,
                GetTimestampFromUuid.Execute(entity.Id),
                entity.ReportedBy.Name
            );
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<List<Response>>>
    {
        public async Task<Result<List<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden("Bạn không có quyền thực hiện hành động này");

            var reports = await context
                .BookingReports.Include(r => r.ReportedBy)
                .Where(r =>
                    r.Status == BookingReportStatus.UnderReview
                    && r.ResolvedById == currentUser.User.Id
                )
                .OrderByDescending(r => r.Id)
                .Select(r => Response.FromEntity(r))
                .ToListAsync(cancellationToken);

            return Result.Success(reports);
        }
    }
}
