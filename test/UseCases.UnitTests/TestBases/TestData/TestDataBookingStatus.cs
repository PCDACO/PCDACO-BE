using Domain.Entities;
using Domain.Enums;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataBookingStatus
{
    private static BookingStatus CreateBookingStatus(string statusName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = statusName };

    public static async Task<List<BookingStatus>> CreateTestBookingStatuses(AppDBContext dBContext)
    {
        var statusNames = Enum.GetValues(typeof(BookingStatusEnum))
            .Cast<BookingStatusEnum>()
            .Select(x => x.ToString())
            .ToList();

        var bookingStatuses = statusNames.Select(CreateBookingStatus).ToList();

        await dBContext.BookingStatuses.AddRangeAsync(bookingStatuses);
        await dBContext.SaveChangesAsync();

        return bookingStatuses;
    }
}
