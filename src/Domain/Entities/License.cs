using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class License : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid EncryptionKeyId { get; set; }
    public string EncryptedLicenseNumber { get; set; } = string.Empty;
    public string LicenseImageFrontUrl { get; set; } = string.Empty;
    public string LicenseImageBackUrl { get; set; } = string.Empty;
    public required string ExpiryDate { get; set; }
    public bool? IsApprove { get; set; } = null!;
    public string? RejectReason { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
}
