using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_InspectionSchedule.Queries;

public sealed class GetAllInspectionSchedules
{
    public sealed record Query(Guid? TechnicianId, MonthEnum? Month = null, int? Year = null)
        : IRequest<Result<IEnumerable<Response>>>;

    public sealed record Response(
        Guid Id,
        Guid TechnicianId,
        string TechnicianName,
        Guid CarId,
        Guid CarOwnerId,
        string CarOwnerName,
        Guid? ReportId,
        string StatusName,
        string Note,
        string InspectionAddress,
        DateTimeOffset InspectionDate,
        InspectionScheduleType Type,
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
                ReportId: schedule.Type == InspectionScheduleType.NewCar
                    ? null
                    : (
                        schedule.Type == InspectionScheduleType.Incident
                            ? schedule.ReportId
                            : schedule.CarReportId
                    ),
                schedule.Status.ToString(),
                schedule.Note,
                schedule.InspectionAddress,
                schedule.InspectionDate,
                Type: schedule.Type,
                GetTimestampFromUuid.Execute(schedule.Id)
            );
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<IEnumerable<Response>>>
    {
        public async Task<Result<IEnumerable<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (
                !currentUser.User!.IsConsultant()
                && !currentUser.User!.IsTechnician()
                && !currentUser.User!.IsOwner()
                && !currentUser.User!.IsAdmin()
            )
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            var query = context
                .InspectionSchedules.AsSplitQuery()
                .Include(s => s.Technician)
                .Include(s => s.Car)
                .ThenInclude(c => c.Owner)
                .Where(s => !s.IsDeleted);

            // Filter by owner if the user is an owner
            if (currentUser.User.IsOwner())
            {
                query = query.Where(s => s.Car.OwnerId == currentUser.User.Id);
            }

            // Filter by technician if provided
            if (request.TechnicianId != null)
            {
                query = query.Where(s => s.TechnicianId == request.TechnicianId);
            }

            // Apply month and year filters
            if (request.Month.HasValue || request.Year.HasValue)
            {
                int year = request.Year ?? DateTimeOffset.UtcNow.Year;

                if (request.Month.HasValue)
                {
                    int month = (int)request.Month.Value;
                    query = query.Where(s =>
                        s.InspectionDate.Month == month && s.InspectionDate.Year == year
                    );
                }
                else
                {
                    query = query.Where(s => s.InspectionDate.Year == year);
                }
            }

            // Default sort by ID ascending
            query = query.OrderBy(s => s.Id);

            var schedules = await query
                .Select(s => Response.FromEntity(s))
                .ToListAsync(cancellationToken);

            return Result.Success((IEnumerable<Response>)schedules, ResponseMessages.Fetched);
        }
    }
}
