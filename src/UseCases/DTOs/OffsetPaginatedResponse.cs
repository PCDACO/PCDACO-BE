namespace UseCases.DTOs;

public record OffsetPaginatedResponse<T>(
    IEnumerable<T> Items,
    int TotalItems,
    int PageNumber,
    int PageSize
)
{
    public static OffsetPaginatedResponse<T> Map
        (IEnumerable<T> items, int totalItems, int pageNumber, int pageSize)
        => new(items, totalItems, pageNumber, pageSize);
};