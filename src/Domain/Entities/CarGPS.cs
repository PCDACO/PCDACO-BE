
using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

using NetTopologySuite.Geometries;

namespace Domain.Entities;

public class CarGPS : BaseEntity
{
    public required Guid DeviceId { get; set; }
    public required Guid CarId { get; set; }
    public required Point Location { get; set; }
    // Navigation
    [ForeignKey(nameof(DeviceId))]
    public GPSDevice Device { get; set; } = null!;
    [ForeignKey(nameof(CarId))]
    public Car Car { get; set; } = null!;
}