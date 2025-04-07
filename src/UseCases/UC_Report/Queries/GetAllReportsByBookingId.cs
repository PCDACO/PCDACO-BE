using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Report.Queries;

public sealed class GetAllReportsByBookingId
{
    public sealed record Query(
        Guid BookingId,
        int PageNumber = 1,
        int PageSize = 10,
        BookingReportStatus? Status = null,
        BookingReportType? Type = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        string Title,
        string Description,
        BookingReportType ReportType,
        BookingReportStatus Status,
        ReporterInfo Reporter,
        CompensationInfo? Compensation,
        ResolutionInfo? Resolution,
        string[] ImageUrls,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(BookingReport report) =>
            new(
                report.Id,
                report.Title,
                report.Description,
                report.ReportType,
                report.Status,
                new ReporterInfo(report.ReportedById, report.ReportedBy.Name),
                report.CompensationAmount.HasValue
                    ? new CompensationInfo(
                        report.CompensationPaidUserId!.Value,
                        report.CompensationPaidUser!.Name,
                        report.CompensationReason!,
                        report.CompensationAmount.Value,
                        report.IsCompensationPaid ?? false,
                        report.CompensationPaidImageUrl,
                        report.CompensationPaidAt
                    )
                    : null,
                report.ResolvedAt.HasValue && report.ResolvedById.HasValue
                    ? new ResolutionInfo(
                        report.ResolvedById.Value,
                        report.ResolvedBy.Name,
                        report.ResolutionComments ?? string.Empty,
                        report.ResolvedAt.Value
                    )
                    : null,
                [.. report.ImageReports.Select(ir => ir.Url)],
                GetTimestampFromUuid.Execute(report.Id)
            );
    }

    public sealed record ReporterInfo(Guid Id, string Name);

    public sealed record CompensationInfo(
        Guid UserId,
        string UserName,
        string Reason,
        decimal Amount,
        bool IsPaid,
        string? PaidImageUrl,
        DateTimeOffset? PaidAt
    );

    public sealed record ResolutionInfo(
        Guid ResolverId,
        string ResolverName,
        string Comments,
        DateTimeOffset ResolvedAt
    );

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get booking and validate access
            var booking = await context
                .Bookings.AsNoTracking()
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy đơn đặt xe");

            // Only allow access for the driver, owner, admin or consultant
            if (
                !currentUser.User!.IsAdmin()
                && !currentUser.User.IsConsultant()
                && booking.UserId != currentUser.User.Id
                && booking.Car.OwnerId != currentUser.User.Id
            )
            {
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            }

            // Build query
            var query = context
                .BookingReports.AsNoTracking()
                .Include(r => r.ReportedBy)
                .Include(r => r.ResolvedBy)
                .Include(r => r.CompensationPaidUser)
                .Include(r => r.ImageReports)
                .Where(r => r.BookingId == request.BookingId);

            // Apply status filter if provided
            if (request.Status.HasValue)
            {
                query = query.Where(r => r.Status == request.Status.Value);
            }

            // Apply type filter if provided
            if (request.Type.HasValue)
            {
                query = query.Where(r => r.ReportType == request.Type.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and get results
            var reports = await query
                .OrderByDescending(r => r.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Check if there are more results
            bool hasNext = await query
                .Skip(request.PageSize * request.PageNumber)
                .AnyAsync(cancellationToken);

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    reports.Select(Response.FromEntity),
                    totalCount,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
