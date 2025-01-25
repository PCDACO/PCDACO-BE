using Domain.Shared;

namespace Domain.Entities;

public class BookingStatus : BaseEntity
{
    public required string Name { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
}