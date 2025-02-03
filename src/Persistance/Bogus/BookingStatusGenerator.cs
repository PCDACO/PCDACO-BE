using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class BookingStatusGenerator
{
    public static BookingStatus[] Execute()
    {
        return
        [
            .. Enum.GetValues(typeof(BookingStatusEnum))
                .Cast<BookingStatusEnum>()
                .Select(status => new BookingStatus() { Name = status.ToString(), })
        ];
    }
}
