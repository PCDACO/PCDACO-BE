using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.SignalR;

namespace UseCases.BackgroundServices.InspectionSchedule;

public class InspectionScheduleExpiredJob(
    IAppDBContext context,
    IHubContext<ScheduleHub> hubContext
)
{
    public async Task ExpireInspectionSchedulesAutomatically()
    {
        var now = DateTimeOffset.UtcNow;

        // Get schedules that need to be expired
        var schedulesToExpire = await context
            .InspectionSchedules.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(s => s.Car)
            .ThenInclude(c => c.Contract)
            .Where(s => !s.IsDeleted)
            .Where(s =>
                // More than 15 minutes past scheduled time and still pending
                (
                    s.InspectionDate.AddMinutes(15) < now
                    && s.Status == InspectionScheduleStatusEnum.Pending
                )
                ||
                // More than 1 hour past scheduled time and neither approved nor rejected
                (
                    s.InspectionDate.AddHours(1) < now
                    && s.Status != InspectionScheduleStatusEnum.Approved
                    && s.Status != InspectionScheduleStatusEnum.Rejected
                )
            )
            .Where(s => s.Status != InspectionScheduleStatusEnum.Expired) // Exclude already expired
            .ToListAsync();

        if (schedulesToExpire.Count == 0)
            return;

        // Update schedules to expired
        foreach (var schedule in schedulesToExpire)
        {
            schedule.Status = InspectionScheduleStatusEnum.Expired;
            schedule.UpdatedAt = now;

            // Reset signature in the associated car contract if it exists
            if (schedule.Car?.Contract != null)
            {
                var contract = schedule.Car.Contract;
                contract.OwnerSignature = null;
                contract.OwnerSignatureDate = null;
                contract.TechnicianSignature = null;
                contract.TechnicianSignatureDate = null;
                contract.Status = CarContractStatusEnum.Pending;
                contract.UpdatedAt = now;
            }
        }

        await context.SaveChangesAsync(CancellationToken.None);

        // update statuses of expired schedules in real-time
        await hubContext.Clients.All.SendAsync(
            "UpdateInspectionScheduleStatus",
            schedulesToExpire.Select(s => s.Id).ToList(),
            InspectionScheduleStatusEnum.Expired
        );
    }
}
