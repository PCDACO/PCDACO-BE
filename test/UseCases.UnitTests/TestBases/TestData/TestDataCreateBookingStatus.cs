using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateBookingStatus
{
    private static BookingStatus CreateBookingStatus(string statusName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = statusName };

    public static async Task<BookingStatus> CreateTestBookingStatus(
        AppDBContext dBContext,
        string statusName
    )
    {
        var bookingStatus = CreateBookingStatus(statusName);

        await dBContext.BookingStatuses.AddAsync(bookingStatus);
        await dBContext.SaveChangesAsync();

        return bookingStatus;
    }

    public static async Task<List<BookingStatus>> CreateTestBookingStatuses(
        AppDBContext dBContext,
        List<string> statusNames
    )
    {
        var bookingStatuses = statusNames
            .Select(statusName => CreateBookingStatus(statusName))
            .ToList();

        await dBContext.BookingStatuses.AddRangeAsync(bookingStatuses);
        await dBContext.SaveChangesAsync();

        return bookingStatuses;
    }

    public static async Task<List<BookingStatus>> InitializeTestBookingStatuses(
        AppDBContext dBContext
    )
    {
        List<string> statusNames = new() { "Pending", "Confirmed", "Cancelled" };

        var bookingStatuses = new List<BookingStatus>();

        for (var i = 0; i < statusNames.Count; i++)
        {
            var bookingStatus = CreateBookingStatus(statusNames[i]);
            bookingStatuses.Add(bookingStatus);
        }

        await dBContext.BookingStatuses.AddRangeAsync(bookingStatuses);
        await dBContext.SaveChangesAsync();

        return bookingStatuses;
    }
}
