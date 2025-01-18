using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class FinancialReport : BaseEntity
{
    public required Guid UserId { get; set; }
    public required decimal Amount { get; set; }
    public FinancialReportEnum Status { get; set; } = FinancialReportEnum.Pending;
    public string Description { get; set; } = string.Empty;
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}