using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class TransactionStatusGenerator
{
    private static readonly string[] _transactionStatus =
    [
        TransactionStatusNames.Pending,
        TransactionStatusNames.Completed,
        TransactionStatusNames.Failed,
        TransactionStatusNames.Refund,
        TransactionStatusNames.Cancelled,
    ];

    public static TransactionStatus[] Execute() =>
        [.. _transactionStatus.Select(status => new TransactionStatus() { Name = status, })];
}
