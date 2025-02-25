using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_InspectionSchedule.Queries;

public sealed class GetInDateScheduleForCurrentTechnician
{
    public sealed record Query(int PageNumber = 1, int PageSize = 10)
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        Guid TechnicianId,
        Guid CarId,
        Guid InspectionStatusId,
        string StatusName,
        DateTimeOffset InspectionDate,
        DateTimeOffset CreateAt
    )
    {
        public static Response FromEntity(InspectionSchedule schedule) =>
            new(
                schedule.Id,
                schedule.TechnicianId,
                schedule.CarId,
                schedule.InspectionStatusId,
                schedule.InspectionStatus.Name,
                schedule.InspectionDate,
                GetTimestampFromUuid.Execute(schedule.CarId)
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
            // Verify current user is technician
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get today's schedules for current technician
            var today = DateTimeOffset.UtcNow.Date;
            var query = context
                .InspectionSchedules.Include(s => s.InspectionStatus)
                .Where(s =>
                    s.TechnicianId == currentUser.User.Id
                    && EF.Functions.ILike(s.InspectionStatus.Name, "%pending%")
                    && !s.IsDeleted
                    && s.InspectionDate.Date == today
                )
                .OrderByDescending(s => s.Id);

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
