using Domain.Entities;

namespace Persistance.Bogus;

public class TransactionStatusGenerator
{
    private static readonly string[] _transactionStatus = ["Pending", "Confirmed", "Cancelled"];
    public static TransactionStatus[] Execute()
    {
        return [.. _transactionStatus.Select(status => {
            return new TransactionStatus()
            {
                Name = status,
            };
        })];
    }
}