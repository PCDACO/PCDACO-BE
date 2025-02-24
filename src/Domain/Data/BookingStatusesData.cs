using Domain.Entities;

namespace Domain.Data;

public class BookingStatusesData
{
    public ICollection<BookingStatus> BookingStatuses { get; private set; } = [];
    public void Set(BookingStatus[] bookingStatuses)
        => BookingStatuses = bookingStatuses;
}