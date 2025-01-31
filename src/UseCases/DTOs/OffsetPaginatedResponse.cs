namespace UseCases.DTOs;

public record OffsetPaginatedResponse<T>(
    IEnumerable<T> Items,
    int TotalItems,
    int PageNumber,
    int PageSize,
    bool HasNext = false
)
{
    public static OffsetPaginatedResponse<T> Map
        (IEnumerable<T> items, int totalItems, int pageNumber, int pageSize, bool hasNext = false)
        => new(items, totalItems, pageNumber, pageSize, hasNext);
};