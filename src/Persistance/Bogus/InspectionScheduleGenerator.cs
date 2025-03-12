using Bogus;
using Domain.Entities;

namespace Persistance.Bogus;

public class InspectionScheduleGenerator
{
    private static readonly Faker _faker = new("vi");

    private static readonly string[] _streetNames =
    [
        "Nguyễn Huệ",
        "Lê Lợi",
        "Trần Hưng Đạo",
        "Điện Biên Phủ",
        "Võ Văn Tần",
        "Nguyễn Thị Minh Khai",
        "Phạm Ngũ Lão",
        "Lý Tự Trọng",
        "Nam Kỳ Khởi Nghĩa",
        "Võ Thị Sáu",
        "Nguyễn Du",
        "Cách Mạng Tháng Tám",
        "Hai Bà Trưng",
        "Lê Duẩn",
        "Tôn Đức Thắng",
    ];

    private static string GenerateVietnameseAddress()
    {
        var districtNumber = _faker.Random.Number(1, 12);
        var wardNumber = _faker.Random.Number(1, 30);
        var streetNumber = _faker.Random.Number(1, 999);
        var streetName = _faker.PickRandom(_streetNames);
        return $"{streetNumber} {streetName}, "
            + $"Phường {wardNumber}, "
            + $"Quận {districtNumber}, "
            + "Thành phố Hồ Chí Minh";
    }

    public static InspectionSchedule[] Execute(
        Car[] cars,
        User[] users,
        UserRole[] roles
    )
    {
        // Find cars in pending status
        var pendingCars = cars.Where(c => c.Status == Domain.Enums.CarStatusEnum.Pending).ToArray();

        if (pendingCars.Length == 0)
        {
            return Array.Empty<InspectionSchedule>();
        }

        //Find the consultant role
        var consultantRole = roles.First(r => r.Name.ToLower() == "consultant");

        // Get all consultants
        var consultants = users.Where(u => u.RoleId == consultantRole.Id).ToArray();

        if (pendingCars.Length == 0 || consultants.Length == 0)
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

            // Randomly pick a consultant
            var consultant = _faker.PickRandom(consultants);

            var schedule = new InspectionSchedule
            {
                TechnicianId = Guid.Parse("01951eae-453b-7ad9-949f-63dd30b592e1"),
                CarId = car.Id,
                InspectionAddress = GenerateVietnameseAddress(),
                InspectionDate = today.AddHours(9 + i), // Schedule from 9 AM with 1 hour intervals
                Note = $"Inspection #{i + 1} for {car.Id}",
                CreatedBy = consultant.Id,
            };

            schedules.Add(schedule);
        }

        return schedules.ToArray();
    }
}