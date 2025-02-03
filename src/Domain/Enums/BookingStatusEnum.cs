namespace Domain.Enums;

public enum BookingStatusEnum
{
    Pending,
    Approved,
    Rejected,
    Ongoing,
    Completed,
    Cancelled
}

public static class BookingStatusExtensions
{
    // Convert string to enum
    public static BookingStatusEnum ToEnum(this string statusName) =>
        (BookingStatusEnum)Enum.Parse(typeof(BookingStatusEnum), statusName);
}
