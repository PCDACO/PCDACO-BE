using Bogus;
using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class BookingReportGenerator
{
    private static readonly Faker _faker = new();

    public static BookingReport[] Execute(Booking[] bookings, User[] users, Car[] cars)
    {
        if (bookings == null || !bookings.Any() || users == null || !users.Any())
        {
            return [];
        }

        // Get completed bookings to report on
        var completedBookings = bookings
            .Where(b => b.Status == BookingStatusEnum.Completed)
            .ToArray();

        if (completedBookings.Length == 0)
        {
            // If no completed bookings, use any bookings
            completedBookings = bookings.Take(Math.Min(5, bookings.Length)).ToArray();
        }

        var reports = new List<BookingReport>();

        // Generate report titles
        var reportTitles = new[]
        {
            "Xe bị trầy xước cửa",
            "Khách hàng trả xe muộn",
            "Nội thất xe bị hư hỏng",
            "Xe bị va chạm nhẹ",
            "Mất đồ trong xe",
            "Xe bị trả không đúng địa điểm",
            "Vấn đề về động cơ sau khi cho thuê",
        };

        // Generate report descriptions
        var reportDescriptions = new[]
        {
            "Sau khi khách hàng trả xe, tôi phát hiện xe bị trầy xước ở cửa bên phải. Trầy xước dài khoảng 10cm và khá sâu.",
            "Khách hàng trả xe muộn 3 tiếng so với thời gian đã thỏa thuận và không thông báo trước.",
            "Nội thất xe bị hư hỏng, ghế sau có vết rách và bị bẩn không thể giặt sạch.",
            "Xe bị va chạm nhẹ ở phần đuôi, đèn hậu bên trái bị vỡ.",
            "Sau khi khách trả xe, tôi phát hiện mất bộ dụng cụ sửa xe và bơm lốp dự phòng.",
            "Khách hàng đã trả xe ở một địa điểm khác so với thỏa thuận ban đầu, gây khó khăn trong việc nhận xe.",
            "Sau khi cho thuê, xe xuất hiện tiếng kêu lạ từ động cơ. Cần kiểm tra kỹ thuật ngay lập tức.",
        };

        // Create at least 3 pending reports
        for (int i = 0; i < Math.Min(completedBookings.Length, 5); i++)
        {
            var booking = completedBookings[i];

            if (cars == null || !cars.Any())
            {
                return [];
            }

            // Find the car for this booking if navigation property not loaded
            var car = cars.FirstOrDefault(c => c.Id == booking.CarId);

            // Determine reporter (randomly choose between driver and owner)
            bool isDriverReporting = _faker.Random.Bool();
            var reporterId = isDriverReporting ? booking.UserId : (car?.OwnerId ?? booking.UserId);

            var report = new BookingReport
            {
                BookingId = booking.Id,
                ReportedById = reporterId,
                Title = reportTitles[i % reportTitles.Length],
                Description = reportDescriptions[i % reportDescriptions.Length],
                ReportType = _faker.PickRandom<BookingReportType>(),
                Status =
                    i < 3
                        ? BookingReportStatus.Pending // First 3 are Pending
                        : _faker.PickRandom(
                            BookingReportStatus.UnderReview,
                            BookingReportStatus.Resolved,
                            BookingReportStatus.Rejected
                        ),
            };

            reports.Add(report);
        }

        return reports.ToArray();
    }
}
