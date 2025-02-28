using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateBooking
{
    private static Booking CreateBooking(
        Guid userId,
        Guid carId,
        Guid statusId,
        DateTime? startTime = null,
        DateTime? endTime = null
    ) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            CarId = carId,
            StatusId = statusId,
            StartTime = startTime ?? DateTime.UtcNow.AddHours(1),
            EndTime = endTime ?? DateTime.UtcNow.AddHours(3),
            ActualReturnTime = DateTime.UtcNow.AddHours(3),
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110m,
            Note = "Test note",
        };

    public static async Task<Booking> CreateTestBooking(
        AppDBContext dBContext,
        Guid userId,
        Guid carId,
        Guid statusId,
        DateTime? startTime = null,
        DateTime? endTime = null
    )
    {
        var booking = CreateBooking(userId, carId, statusId, startTime, endTime);

        await dBContext.Bookings.AddAsync(booking);
        await dBContext.SaveChangesAsync();

        return booking;
    }
}
