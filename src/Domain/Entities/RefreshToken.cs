using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class RefreshToken
{
    [Key]
    public required string Token { get; set; } = Guid.NewGuid().ToString();
    public required Guid UserId { get; set; }
    public required DateTimeOffset ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = true;
    public bool IsRevoked { get; set; } = false;
    public DateTimeOffset? RevokedAt { get; set; }
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}