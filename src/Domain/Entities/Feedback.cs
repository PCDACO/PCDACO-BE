using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class Feedback : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid BookingId { get; set; }
    public int Point { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
    public FeedbackTypeEnum Type { get; set; } = FeedbackTypeEnum.Driver;
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
    public ICollection<ImageFeedback> ImageFeedbacks { get; set; } = null!;
}