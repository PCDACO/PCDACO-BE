using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;

namespace Domain.Entities;

public class ImageFeedback : BaseEntity
{
    public required Guid FeedbackId { get; set; }
    public required string Url { get; set; }
    // Navigation Properties
    [ForeignKey(nameof(FeedbackId))]
    public Feedback Feedback { get; set; } = null!;
}