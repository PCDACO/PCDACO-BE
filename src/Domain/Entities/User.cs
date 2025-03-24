using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class User : BaseEntity
{
    // Properties
    public required Guid EncryptionKeyId { get; set; }
    public required Guid RoleId { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Address { get; set; }
    public required DateTimeOffset DateOfBirth { get; set; }
    public required string Phone { get; set; }
    public decimal Balance { get; set; } = 0;
    public string EncryptedLicenseNumber { get; set; } = string.Empty;
    public string LicenseImageFrontUrl { get; set; } = string.Empty;
    public string LicenseImageBackUrl { get; set; } = string.Empty;
    public DateTimeOffset? LicenseExpiryDate { get; set; }
    public bool? LicenseIsApproved { get; set; } = null!;
    public string? LicenseRejectReason { get; set; }
    public DateTimeOffset? LicenseImageUploadedAt { get; set; }
    public DateTimeOffset? LicenseApprovedAt { get; set; }
    public bool IsBanned { get; set; } = false;
    public string BannedReason { get; set; } = string.Empty;

    [Range(1, 5)]
    // Navigation Properties
    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public UserRole Role { get; set; } = null!;
    public UserStatistic UserStatistic { get; set; } = null!;

    [InverseProperty(nameof(WithdrawalRequest.User))]
    public ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<BankAccount> BankAccounts { get; set; } = [];
    public ICollection<Car> Cars { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
    public ICollection<CarContract> CarContracts { get; set; } = [];

    [InverseProperty(nameof(Transaction.FromUser))]
    public ICollection<Transaction> SentTransactions { get; set; } = [];

    [InverseProperty(nameof(Transaction.ToUser))]
    public ICollection<Transaction> ReceivedTransactions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    [InverseProperty(nameof(InspectionSchedule.Technician))]
    public ICollection<InspectionSchedule> TechnicianInspectionSchedules { get; set; } = [];

    [InverseProperty(nameof(InspectionSchedule.Consultant))]
    public ICollection<InspectionSchedule> ConsultantInspectionSchedules { get; set; } = [];

    public bool IsAdmin() => Role.Name == "Admin";

    public bool IsDriver() => Role.Name == "Driver";

    public bool IsOwner() => Role.Name == "Owner";

    public bool IsConsultant() => Role.Name == "Consultant";

    public bool IsTechnician() => Role.Name == "Technician";
}
