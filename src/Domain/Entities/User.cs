using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class User : BaseEntity
{
    // Properties
    public required Guid EncryptionKeyId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Address { get; set; }
    public required DateTimeOffset DateOfBirth { get; set; }
    public required string Phone { get; set; }
    [Range(1, 5)]
    public decimal Rating { get; set; } = 5;
    // Navigation Properties
    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
    public UserStatistic UserStatistic { get; set; } = null!;
    public FinancialReport FinancialReport { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<BankAccount> BankAccounts { get; set; } = [];
    public ICollection<Car> Cars { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}