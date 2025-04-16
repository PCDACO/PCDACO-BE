using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;

namespace Domain.Entities;

public class ImageReport : BaseEntity
{
    public Guid? BookingReportId { get; set; }
    public Guid? CarReportId { get; set; }
    public required string Url { get; set; }

    [ForeignKey(nameof(BookingReportId))]
    public BookingReport? BookingReport { get; set; }

    [ForeignKey(nameof(CarReportId))]
    public CarReport? CarReport { get; set; }
}
