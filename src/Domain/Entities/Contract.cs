using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class Contract : BaseEntity
{
    // Properties
    public required Guid BookingId { get; set; }
    public ContractStatusEnum Status { get; set; } = ContractStatusEnum.Pending;
    public required DateTimeOffset StartDate { get; set; }
    public required DateTimeOffset EndDate { get; set; }
    public string Terms { get; set; } = string.Empty;
    public DateTimeOffset? DriverSignatureDate { get; set; }
    public DateTimeOffset? OwnerSignatureDate { get; set; }
    public string? DriverSignature { get; set; }
    public string? OwnerSignature { get; set; }
    public string? PickupAddress { get; set; }
    public decimal RentalPrice { get; set; }
    public int RentalPeriod => (EndDate - StartDate).Days;

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}
