using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Activities.Queries;

public class GetTechnicianRecentActivity
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(ActivityDetails[] Activities)
    {
        public static async Task<Response> FromEntity(
            InspectionSchedule[] schedules,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService)
        {
            var activities = await Task.WhenAll(
                schedules.Select(async schedule =>
                {
                    // Decrypt the key
                    string decryptedKey = keyManagementService.DecryptKey(
                        schedule.Car.EncryptionKey.EncryptedKey,
                        masterKey);

                    // Decrypt the license plate asynchronously
                    string decryptedLicensePlate = await aesEncryptionService.Decrypt(
                        schedule.Car.EncryptedLicensePlate,
                        decryptedKey,
                        schedule.Car.EncryptionKey.IV);

                    // Map the schedule to the message content
                    string content = MapScheduleToMessages(schedule, decryptedLicensePlate);

                    // Return the Activity instance; adjust the constructor as needed.
                    return new ActivityDetails(
                        AvatarUrl: schedule.Technician.AvatarUrl,
                        Content: content,
                        HappenedAt: schedule.UpdatedAt ?? GetTimestampFromUuid.Execute(schedule.Id)
                    );
                }));

            // Return the response with the resulting activities.
            return new Response(Activities: activities);
        }

        private static string MapScheduleToMessages(InspectionSchedule schedule, string licensePlate)
        {
            switch (schedule.Status)
            {
                case Domain.Enums.InspectionScheduleStatusEnum.Approved:
                    {
                        return $"Kĩ thuật viên {schedule.Technician.Name} đã hoàn tất kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name}";
                    };
                case Domain.Enums.InspectionScheduleStatusEnum.Expired:
                    {
                        return $"Kĩ thuật viên {schedule.Technician.Name} đã quá hạn kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name}";
                    };
                case Domain.Enums.InspectionScheduleStatusEnum.InProgress:
                    {
                        return $"Kĩ thuật viên {schedule.Technician.Name} đã cập nhật đang trong quá trình kiểm định xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name}";
                    };
                case Domain.Enums.InspectionScheduleStatusEnum.Rejected:
                    {
                        return $"Kĩ thuật viên {schedule.Technician.Name} đã từ chối xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name}";
                    };
                case Domain.Enums.InspectionScheduleStatusEnum.Signed:
                    {
                        return $"Kĩ thuật viên {schedule.Technician.Name} đã kí hợp đồng thuê xe {schedule.Car.Model.Name}-{licensePlate} của {schedule.Car.Owner.Name}";
                    };
                default:
                    {
                        return "";
                    }
            }
        }
    }

    public record ActivityDetails(
        string AvatarUrl,
    string Content,
    DateTimeOffset HappenedAt
            );

    internal sealed class Handler(
            IAppDBContext context,
            CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
            ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!currentUser?.User?.IsTechnician() ?? false)
            {
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            }
            InspectionSchedule[] activities = await context.InspectionSchedules
                .AsNoTracking()
                .AsSplitQuery()
                .Include(i => i.Car).ThenInclude(c => c.Owner).ThenInclude(o => o.EncryptionKey)
                .Include(i => i.Car).ThenInclude(o => o.EncryptionKey)
                .Include(i => i.Car).ThenInclude(c => c.Model)
                .Include(i => i.Technician)
                .Where(i => !i.IsDeleted)
                .Where(i => i.UpdatedAt != null)
                .Where(i => i.TechnicianId == currentUser!.User!.Id)
                .OrderByDescending(i => i.UpdatedAt).ThenByDescending(i => i.Id)
                .Take(5)
                .ToArrayAsync(cancellationToken);
            return Result.Success(await Response.FromEntity(
                            activities,
                            encryptionSettings.Key,
                            aesEncryptionService,
                            keyManagementService
                            ), ResponseMessages.Fetched);
        }
    }

}