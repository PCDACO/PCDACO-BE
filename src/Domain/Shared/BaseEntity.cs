using UUIDNext;

namespace Domain.Shared;

public class BaseEntity
{
    public Guid Id { get; set; } = Uuid.NewDatabaseFriendly(Database.PostgreSql);
    public DateTimeOffset? UpdatedAt { get; set; } = null!;
    public DateTimeOffset? DeletedAt { get; set; } = null!;
    public bool IsDeleted { get; set; } = false;

    public void Delete()
    {
        UpdatedAt = DateTime.UtcNow;
        DeletedAt = DateTime.UtcNow;
        IsDeleted = true;
    }

    public void Restore()
    {
        UpdatedAt = DateTime.UtcNow;
        DeletedAt = null;
        IsDeleted = false;
    }
}