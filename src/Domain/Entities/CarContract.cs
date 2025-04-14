using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class CarContract : BaseEntity
{
    public required Guid CarId { get; set; }
    public Guid? TechnicianId { get; set; }
    public Guid? GPSDeviceId { get; set; }
    public string Terms { get; set; } = string.Empty;
    public DateTimeOffset? OwnerSignatureDate { get; set; }
    public DateTimeOffset? TechnicianSignatureDate { get; set; }
    public string? OwnerSignature { get; set; }
    public string? TechnicianSignature { get; set; }
    public string? InspectionResults { get; set; }
    public CarContractStatusEnum Status { get; set; } = CarContractStatusEnum.Pending;

    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;

    [ForeignKey(nameof(TechnicianId))]
    public User? Technician { get; set; }

    [ForeignKey(nameof(GPSDeviceId))]
    public GPSDevice? GPSDevice { get; set; }
}
