using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class FinancialReport : BaseEntity
{
    public required Guid UserId { get; set; }
    public required decimal Amount { get; set; }
    public FinancialReportType Type { get; set; } = FinancialReportType.Income;
    public FinancialReportStatus Status { get; set; } = FinancialReportStatus.Pending;
    public string Description { get; set; } = string.Empty;
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}