using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class User : BaseEntity
{
    // Properties
    public required Guid EncryptionKeyId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public UserRole Role { get; set; } = UserRole.Driver;
    public required string Address { get; set; }
    public required DateTimeOffset DateOfBirth { get; set; }
    public required string Phone { get; set; }
    public decimal Balance { get; set; } = 0;

    [Range(1, 5)]
    // Navigation Properties
    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
    public UserStatistic UserStatistic { get; set; } = null!;
    public WithdrawalRequest WithdrawalRequest { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<BankAccount> BankAccounts { get; set; } = [];
    public ICollection<Car> Cars { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];

    [InverseProperty(nameof(Transaction.FromUser))]
    public ICollection<Transaction> SentTransactions { get; set; } = [];

    [InverseProperty(nameof(Transaction.ToUser))]
    public ICollection<Transaction> ReceivedTransactions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public bool IsAdmin() => Role == UserRole.Admin;

    public bool IsDriver() => Role == UserRole.Driver;

    public bool IsOwner() => Role == UserRole.Owner;
}
