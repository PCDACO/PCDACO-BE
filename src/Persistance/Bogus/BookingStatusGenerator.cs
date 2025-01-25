using Domain.Entities;
namespace Persistance.Bogus;

public class BookingStatusGenerator
{
    private static readonly string[] _bookingStatus = ["Pending", "Confirmed", "Cancelled"];
    public static BookingStatus[] Execute()
    {
        return [.. _bookingStatus.Select(status => {
            return new BookingStatus()
            {
                Name = status,
            };
        })];
    }
}