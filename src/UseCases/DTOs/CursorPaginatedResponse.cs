namespace UseCases.DTOs;

public record CursorPaginatedResponse<T>(
    IEnumerable<T> Items,
    int TotalItems,
    int Limit,
    Guid? LastId,
    bool HasMore
)
{
    public static CursorPaginatedResponse<T> Create(
        IEnumerable<T> items,
        int totalItems,
        int limit,
        Guid? lastId,
        bool hasMore = false
    ) => new(items, totalItems, limit, lastId, hasMore);
};
