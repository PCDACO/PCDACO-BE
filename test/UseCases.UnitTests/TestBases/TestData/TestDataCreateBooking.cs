using Domain.Entities;
using Domain.Enums;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateBooking
{
    private static Booking CreateBooking(
        Guid userId,
        Guid carId,
        BookingStatusEnum status,
        DateTime? startTime = null,
        DateTime? endTime = null,
        bool? isPaid = false
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            CarId = carId,
            Status = status,
            StartTime = startTime ?? DateTime.UtcNow.AddHours(1),
            EndTime = endTime ?? DateTime.UtcNow.AddHours(3),
            ActualReturnTime = DateTime.UtcNow.AddHours(3),
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110m,
            IsPaid = isPaid ?? false,
            Note = "Test note",
        };

    public static async Task<Booking> CreateTestBooking(
        AppDBContext dBContext,
        Guid userId,
        Guid carId,
        BookingStatusEnum status,
        DateTime? startTime = null,
        DateTime? endTime = null,
        bool? isPaid = false
    )
    {
        var booking = CreateBooking(userId, carId, status, startTime, endTime, isPaid);

        await dBContext.Bookings.AddAsync(booking);
        await dBContext.SaveChangesAsync();

        return booking;
    }
}
