using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_InspectionSchedule.Queries;

public sealed class GetAllInspectionSchedules
{
    public sealed record Query(
        int PageNumber = 1,
        int PageSize = 10,
        string? Keyword = null,
        DateTimeOffset? InspectionDate = null,
        string SortOrder = "desc"
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        Guid TechnicianId,
        string TechnicianName,
        Guid CarId,
        Guid CarOwnerId,
        string CarOwnerName,
        Guid InspectionStatusId,
        string StatusName,
        string Note,
        string InspectionAddress,
        DateTimeOffset InspectionDate,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(InspectionSchedule schedule) =>
            new(
                schedule.Id,
                schedule.TechnicianId,
                schedule.Technician.Name,
                schedule.CarId,
                schedule.Car.OwnerId,
                schedule.Car.Owner.Name,
                schedule.InspectionStatusId,
                schedule.InspectionStatus.Name,
                schedule.Note,
                schedule.InspectionAddress,
                schedule.InspectionDate,
                GetTimestampFromUuid.Execute(schedule.Id)
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
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            var query = context
                .InspectionSchedules.AsSplitQuery()
                .Include(s => s.InspectionStatus)
                .Include(s => s.Technician)
                .Include(s => s.Car)
                .ThenInclude(c => c.Owner)
                .Where(s => !s.IsDeleted);

            // Apply keyword search for technician name or car owner name or inspection address
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                query = query.Where(s =>
                    EF.Functions.ILike(s.Technician.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(s.Car.Owner.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(s.InspectionAddress, $"%{request.Keyword}%")
                );
            }

            // Apply date filter
            if (request.InspectionDate.HasValue)
                query = query.Where(s =>
                    s.InspectionDate.Date == request.InspectionDate.Value.Date
                );

            // Apply sorting
            query =
                request.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(s => s.Id)
                    : query.OrderByDescending(s => s.Id);

            // Get total count
            var count = await query.CountAsync(cancellationToken);

            // Get paginated data
            var schedules = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => Response.FromEntity(s))
                .ToListAsync(cancellationToken);

            // Check if there are more items
            var hasNext = await query
                .Skip(request.PageNumber * request.PageSize)
                .AnyAsync(cancellationToken);

            return Result.Success(
                new OffsetPaginatedResponse<Response>(
                    schedules,
                    count,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                )
            );
        }
    }
}
