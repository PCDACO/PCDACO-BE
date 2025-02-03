using Domain.Shared;

namespace Domain.Entities;

public class WithdrawalRequestStatus : BaseEntity
{
    // Properties
    public required string Name { get; set; }
    // Navigation properties
    public ICollection<WithdrawalRequest> WithdrawlRequests { get; set; } = [];
}