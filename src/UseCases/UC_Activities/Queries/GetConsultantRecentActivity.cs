using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Activities.Queries;

public class GetConsultantRecentActivity
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(ActivityDetails[] Activities)
    {
        public static async Task<Response> FromEntity(
            (InspectionSchedule[] schedules, BookingReport[] reports) data,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            var activities = new List<ActivityDetails>();

            // Handle inspection schedules
            foreach (var schedule in data.schedules)
            {
                // Map the schedule to the message content
                string content = MapScheduleToMessages(schedule, schedule.Car.LicensePlate);

                activities.Add(
                    new ActivityDetails(
                        AvatarUrl: schedule.Technician.AvatarUrl,
                        Content: content,
                        HappenedAt: schedule.UpdatedAt ?? GetTimestampFromUuid.Execute(schedule.Id),
                        Type: "inspection"
                    )
                );
            }

            // Handle reports
            foreach (var report in data.reports)
            {
                // Map the report to the message content
                string content = MapReportToMessages(report, report.Booking.Car.LicensePlate);

                activities.Add(
                    new ActivityDetails(
                        AvatarUrl: report.ReportedBy.AvatarUrl,
                        Content: content,
                        HappenedAt: report.UpdatedAt ?? GetTimestampFromUuid.Execute(report.Id),
                        Type: "report"
                    )
                );
            }

            // Sort all activities by timestamp descending and take only 10 most recent
            var sortedActivities = activities
                .OrderByDescending(a => a.HappenedAt)
                .Take(10)
                .ToArray();

            return new Response(Activities: sortedActivities);
        }

        private static string MapScheduleToMessages(
            InspectionSchedule schedule,
            string licensePlate
        )
        {
            return schedule.Status switch
            {
                InspectionScheduleStatusEnum.Pending
                    => $"Lịch kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name} đang chờ xử lý",
                InspectionScheduleStatusEnum.Approved
                    => $"Lịch kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name} đã được hoàn tất bởi kĩ thuật viên {schedule.Technician.Name}",
                InspectionScheduleStatusEnum.Expired
                    => $"Lịch kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name} đã quá hạn",
                InspectionScheduleStatusEnum.InProgress
                    => $"Lịch kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name} đang được thực hiện bởi kĩ thuật viên {schedule.Technician.Name}",
                InspectionScheduleStatusEnum.Rejected
                    => $"Lịch kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name} đã bị từ chối bởi kĩ thuật viên {schedule.Technician.Name}",
                InspectionScheduleStatusEnum.Signed
                    => $"Lịch kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name} đã được kí hợp đồng bởi kĩ thuật viên {schedule.Technician.Name}",
                _ => "",
            };
        }

        private static string MapReportToMessages(BookingReport report, string licensePlate)
        {
            return report.Status switch
            {
                BookingReportStatus.Pending
                    => $"Báo cáo mới '{report.Title}' cho xe {report.Booking.Car.Model.Name}-{licensePlate} đang chờ xử lý",
                BookingReportStatus.UnderReview
                    => $"Báo cáo '{report.Title}' cho xe {report.Booking.Car.Model.Name}-{licensePlate} đang được xử lý",
                BookingReportStatus.Resolved
                    => $"Báo cáo '{report.Title}' cho xe {report.Booking.Car.Model.Name}-{licensePlate} đã được giải quyết",
                BookingReportStatus.Rejected
                    => $"Báo cáo '{report.Title}' cho xe {report.Booking.Car.Model.Name}-{licensePlate} đã bị từ chối",
                _ => "",
            };
        }
    }

    public record ActivityDetails(
        string AvatarUrl,
        string Content,
        DateTimeOffset HappenedAt,
        string Type
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get the 10 most recent inspection schedules and reports combined
            var schedules = await context
                .InspectionSchedules.AsNoTracking()
                .AsSplitQuery()
                .Include(i => i.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(i => i.Car)
                .ThenInclude(c => c.Model)
                .Include(i => i.Technician)
                .Where(i => i.CreatedBy == currentUser.User.Id)
                .OrderByDescending(i => i.UpdatedAt)
                .Take(10)
                .ToArrayAsync(cancellationToken);

            var reports = await context
                .BookingReports.AsNoTracking()
                .AsSplitQuery()
                .Include(r => r.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(r => r.Booking)
                .Include(r => r.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(r => r.ReportedBy)
                .Where(r => r.ResolvedById == currentUser.User.Id)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(10)
                .ToArrayAsync(cancellationToken);

            return Result.Success(
                await Response.FromEntity(
                    (schedules, reports),
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
