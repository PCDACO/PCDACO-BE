using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Report.Queries;

public class GetAllReports
{
    public sealed record Query(
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = "",
        BookingReportStatus? Status = null,
        BookingReportType? Type = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        Guid BookingId,
        Guid ReporterId,
        string ReportedName,
        string Title,
        string Description,
        BookingReportType ReportType,
        BookingReportStatus Status,
        DateTimeOffset? ResolvedAt,
        Guid? ResolvedById,
        string? ResolutionComments,
        ICollection<ImageReport> ImageReports
    )
    {
        public static Response FromEntity(BookingReport report) =>
            new(
                report.Id,
                report.BookingId,
                report.ReportedById,
                report.ReportedBy.Name,
                report.Title,
                report.Description,
                report.ReportType,
                report.Status,
                report.ResolvedAt,
                report.ResolvedById,
                report.ResolutionComments,
                report.ImageReports
            );
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = context
                .BookingReports.Include(r => r.ReportedBy)
                .Include(r => r.ImageReports)
                .Include(r => r.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Owner)
                .AsQueryable();

            // Filter by user role
            if (currentUser.User!.IsDriver())
            {
                query = query.Where(r =>
                    r.ReportedById == currentUser.User.Id || r.Booking.UserId == currentUser.User.Id
                );
            }
            else if (currentUser.User.IsOwner())
            {
                query = query.Where(r =>
                    r.ReportedById == currentUser.User.Id
                    || r.Booking.Car.OwnerId == currentUser.User.Id
                );
            }
            // Admin and Consultants can see all reports (no filtering needed)

            // Apply filters
            if (request.Status.HasValue)
            {
                query = query.Where(r => r.Status == request.Status.Value);
            }

            if (request.Type.HasValue)
            {
                query = query.Where(r => r.ReportType == request.Type.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(r =>
                    EF.Functions.ILike(r.Title, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(r.Description, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(r.ReportedBy.Name, $"%{request.SearchTerm}%")
                );
            }

            // Order by Id descending (newest first)
            query = query.OrderByDescending(r => r.Id);

            var totalCount = await query.CountAsync(cancellationToken);

            var reports = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var hasNext = await query
                .Skip(request.PageSize * request.PageNumber)
                .AnyAsync(cancellationToken: cancellationToken);

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    reports.Select(Response.FromEntity),
                    totalCount,
                    request.PageSize,
                    request.PageNumber,
                    hasNext
                ),
                "Lấy danh sách báo cáo thành công"
            );
        }
    }
}
