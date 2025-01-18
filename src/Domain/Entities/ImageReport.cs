using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class ImageReport : BaseEntity
{
    public required Guid CarReportId { get; set; }
    public required string Url { get; set; }
    // Navigation Properties
    [ForeignKey(nameof(CarReportId))]
    public CarReport CarReport { get; set; } = null!;
}