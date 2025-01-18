using Domain.Shared;

namespace Domain.Entities;

public class EncryptionKey : BaseEntity
{
    public required string EncryptedKey { get; set; }
    public string IV { get; set; } = string.Empty;
    // Navigation Properties
    public User User { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
    public Car Car { get; set; } = null!;
}