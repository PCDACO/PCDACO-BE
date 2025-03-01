using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class Contract : BaseEntity
{
    // Properties
    public required Guid BookingId { get; set; }
    public required Guid StatusId { get; set; }
    public required DateTimeOffset StartDate { get; set; }
    public required DateTimeOffset EndDate { get; set; }
    public string Terms { get; set; } = string.Empty;
    public DateTimeOffset? DriverSignatureDate { get; set; }
    public DateTimeOffset? OwnerSignatureDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StatusId))]
    public ContractStatus Status { get; set; } = null!;

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}
