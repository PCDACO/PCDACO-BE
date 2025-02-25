using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class InspectionScheduleGenerator
{
    public static InspectionSchedule[] Execute(
        Car[] cars,
        InspectionStatus[] statuses,
        CarStatus[] carStatuses
    )
    {
        // Find the pending status
        var pendingStatus = statuses.First(s => s.Name.ToLower() == "pending");

        // Find the pending car status
        var pendingCarStatus = carStatuses.First(s => s.Name == CarStatusNames.Pending);

        // Find cars in pending status
        var pendingCars = cars.Where(c => c.StatusId == pendingCarStatus.Id).ToArray();

        if (pendingCars.Length == 0)
        {
            return Array.Empty<InspectionSchedule>();
        }

        // Generate 15 schedules for today
        var today = DateTimeOffset.UtcNow;
        var schedules = new List<InspectionSchedule>();

        for (int i = 0; i < 15; i++)
        {
            // Cycle through pending cars if there are fewer than 15
            var car = pendingCars[i % pendingCars.Length];

            var schedule = new InspectionSchedule
            {
                TechnicianId = Guid.Parse("01951eae-453b-7ad9-949f-63dd30b592e1"),
                CarId = car.Id,
                InspectionStatusId = pendingStatus.Id,
                InspectionDate = today.AddHours(9 + i), // Schedule from 9 AM with 1 hour intervals
                Note = $"Inspection #{i + 1} for {car.Id}",
            };

            schedules.Add(schedule);
        }

        return schedules.ToArray();
    }
}
