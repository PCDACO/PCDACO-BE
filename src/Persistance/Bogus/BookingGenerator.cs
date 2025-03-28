using Bogus;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class BookingGenerator
{
    private static readonly Faker _faker = new();
    private const decimal PLATFORM_FEE_RATE = 0.1m;

    public static Booking[] Execute(
        User[] drivers,
        Car[] cars,
        UserRole[] userRoles,
        int count = 20
    )
    {
        if (drivers == null || !drivers.Any() || cars == null || !cars.Any())
        {
            return [];
        }

        var driverRoleId = userRoles.FirstOrDefault(r => r.Name == UserRoleNames.Driver)?.Id;

        if (driverRoleId == null)
            return [];

        // Filter drivers by role ID and license approval
        var driversWithLicense = drivers
            .Where(d => d.RoleId == driverRoleId && d.LicenseIsApproved == true)
            .ToArray();

        if (!driversWithLicense.Any())
            return [];

        var bookings = new List<Booking>();
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            // Select random driver and car
            var driver = driversWithLicense[random.Next(driversWithLicense.Length)];
            var car = cars[random.Next(cars.Length)];

            // Generate random dates
            var startTime = DateTimeOffset.UtcNow.AddDays(random.Next(-30, 30));
            var durationDays = random.Next(1, 7);
            var endTime = startTime.AddDays(durationDays);

            // For completed/cancelled bookings, set them in the past
            var isPastBooking = startTime < DateTimeOffset.UtcNow;

            // Generate random status with appropriate distribution
            var status = GenerateBookingStatus(isPastBooking);

            // Calculate prices
            var basePrice = car.Price * durationDays;
            var platformFee = basePrice * PLATFORM_FEE_RATE;
            var totalAmount = basePrice + platformFee;

            // Additional fields based on status
            decimal totalDistance = 0;
            decimal excessDay = 0;
            decimal excessDayFee = 0;
            bool isPaid = false;
            bool isRefund = false;
            decimal? refundAmount = null;
            DateTimeOffset? refundDate = null;
            DateTimeOffset actualReturnTime = endTime;
            string note = "";

            // Adjust fields based on status
            switch (status)
            {
                case BookingStatusEnum.Completed:
                    totalDistance = random.Next(10, 500) * 1000; // in meters
                    isPaid = true;

                    // Sometimes add excess days
                    if (random.Next(100) < 30)
                    {
                        excessDay = random.Next(1, 3);
                        excessDayFee = car.Price * excessDay * 1.2m;
                        totalAmount += excessDayFee;
                        actualReturnTime = endTime.AddDays((double)excessDay);
                    }
                    // Sometimes add early return with refund
                    else if (random.Next(100) < 20)
                    {
                        actualReturnTime = startTime.AddDays(durationDays / 2.0);
                        decimal unusedDays =
                            durationDays - (decimal)(actualReturnTime - startTime).TotalDays;
                        isRefund = true;
                        refundAmount = car.Price * unusedDays * 0.5m;
                        refundDate = actualReturnTime.AddDays(random.Next(1, 5));
                        totalAmount -= refundAmount.Value;
                    }
                    break;

                case BookingStatusEnum.Cancelled:
                    // Calculate refund based on cancellation time (similar to CancelBooking.cs logic)
                    var daysUntilStart = (startTime - DateTimeOffset.UtcNow).TotalDays;
                    decimal refundPercentage = CalculateRefundPercentage(daysUntilStart);

                    isPaid = random.Next(100) < 50; // 50% chance of being paid before cancellation
                    if (isPaid)
                    {
                        isRefund = true;
                        refundAmount = totalAmount * refundPercentage;
                        refundDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 10));
                    }
                    note = "Cancelled due to " + _faker.Lorem.Sentence(3);
                    break;

                case BookingStatusEnum.Approved:
                case BookingStatusEnum.ReadyForPickup:
                    isPaid = random.Next(100) < 80; // 80% chance of being paid after approval
                    break;

                case BookingStatusEnum.Ongoing:
                    isPaid = true; // Always paid when ongoing
                    break;
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = driver.Id,
                CarId = car.Id,
                Status = status,
                StartTime = startTime,
                EndTime = endTime,
                ActualReturnTime = actualReturnTime,
                BasePrice = basePrice,
                PlatformFee = platformFee,
                ExcessDay = excessDay,
                ExcessDayFee = excessDayFee,
                TotalAmount = totalAmount,
                TotalDistance = totalDistance,
                Note = note,
                IsCarReturned = status == BookingStatusEnum.Completed,
                PayOSOrderCode = isPaid ? random.Next(100000, 999999) : null,
                IsPaid = isPaid,
                IsRefund = isRefund,
                RefundAmount = refundAmount,
                RefundDate = refundDate,
                UpdatedAt = isPastBooking ? actualReturnTime : null,
            };

            bookings.Add(booking);
        }

        return bookings.ToArray();
    }

    private static BookingStatusEnum GenerateBookingStatus(bool isPastBooking)
    {
        var random = new Random();

        // For past bookings, use mostly completed or cancelled status
        if (isPastBooking)
        {
            var pastDistribution = new[]
            {
                BookingStatusEnum.Completed,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Cancelled,
                BookingStatusEnum.Cancelled,
                BookingStatusEnum.Expired,
                BookingStatusEnum.Rejected,
            };
            return pastDistribution[random.Next(pastDistribution.Length)];
        }

        // For future bookings, use pending, approved, or ongoing
        var futureDistribution = new[]
        {
            BookingStatusEnum.Pending,
            BookingStatusEnum.Pending,
            BookingStatusEnum.Approved,
            BookingStatusEnum.Approved,
            BookingStatusEnum.Ongoing,
            BookingStatusEnum.ReadyForPickup,
        };
        return futureDistribution[random.Next(futureDistribution.Length)];
    }

    private static decimal CalculateRefundPercentage(double daysUntilStart)
    {
        const decimal REFUND_PERCENTAGE_BEFORE_7_DAYS = 1.0m;
        const decimal REFUND_PERCENTAGE_BEFORE_5_DAYS = 0.5m;
        const decimal REFUND_PERCENTAGE_BEFORE_3_DAYS = 0.3m;
        const decimal REFUND_PERCENTAGE_BEFORE_1_DAY = 0m;

        return daysUntilStart switch
        {
            >= 7 => REFUND_PERCENTAGE_BEFORE_7_DAYS,
            >= 5 => REFUND_PERCENTAGE_BEFORE_5_DAYS,
            >= 3 => REFUND_PERCENTAGE_BEFORE_3_DAYS,
            _ => REFUND_PERCENTAGE_BEFORE_1_DAY,
        };
    }
}
