using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class Driver : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid EncryptionKeyId { get; set; }
    public string EncryptedLicenseNumber { get; set; } = string.Empty;
    public required string LicenseImageFrontUrl { get; set; }
    public required string LicenseImageBackUrl { get; set; }
    public required string Fullname { get; set; }
    public required string ExpiryDate { get; set; }
    public bool? IsApprove { get; set; } = null!;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
}
