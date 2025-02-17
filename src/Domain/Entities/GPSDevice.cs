using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class GPSDevice : BaseEntity
{
    public required Guid StatusId { get; set; }
    public required string Name { get; set; }
    // Navigation
    public CarGPS GPS { get; set; } = null!;
    [ForeignKey(nameof(StatusId))]
    public DeviceStatus Status { get; set; } = null!;
}