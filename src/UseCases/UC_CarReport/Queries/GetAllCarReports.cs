using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CarReport.Queries;

public class GetAllCarReports
{
    public sealed record Query(
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = "",
        CarReportStatus? Status = null,
        CarReportType? Type = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        Guid CarId,
        Guid ReporterId,
        string ReporterName,
        string Title,
        string Description,
        CarReportType ReportType,
        CarReportStatus Status,
        DateTimeOffset? ResolvedAt,
        Guid? ResolvedById,
        string? ResolutionComments,
        string[] ImageReports
    )
    {
        public static Response FromEntity(CarReport report) =>
            new(
                report.Id,
                report.CarId,
                report.ReportedById,
                report.ReportedBy.Name,
                report.Title,
                report.Description,
                report.ReportType,
                report.Status,
                report.ResolvedAt,
                report.ResolvedById,
                report.ResolutionComments,
                [.. report.ImageReports.Select(ir => ir.Url)]
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
                .CarReports.Include(r => r.ReportedBy)
                .Include(r => r.ImageReports)
                .Include(r => r.Car)
                .ThenInclude(c => c.Owner)
                .AsQueryable();

            // Filter by user role
            if (currentUser.User!.IsOwner())
            {
                query = query.Where(r => r.Car.OwnerId == currentUser.User.Id);
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
