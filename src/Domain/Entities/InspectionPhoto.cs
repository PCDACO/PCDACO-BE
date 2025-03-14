using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class InspectionPhoto : BaseEntity
{
    public required Guid InspectionId { get; set; }
    public required InspectionPhotoType Type { get; set; }
    public required string PhotoUrl { get; set; }
    public string Description { get; set; } = string.Empty;

    [ForeignKey(nameof(InspectionId))]
    public CarInspection Inspection { get; set; } = null!;
}
