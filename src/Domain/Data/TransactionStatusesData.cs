using Domain.Entities;

namespace Domain.Data;

public class TransactionStatusesData
{
    public ICollection<TransactionStatus> TransactionStatuses { get; private set; } = [];
    public void Set(TransactionStatus[] statuses) => TransactionStatuses = statuses;
}