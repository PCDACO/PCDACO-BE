using Domain.Entities;

namespace Persistance.Bogus;

public class TransactionTypeGenerator
{
    private static readonly string[] _transactionTypes = ["BookingPayment", "Withdrawal", "IncomeTransfer", "PlatformFee"];
    public static TransactionType[] Execute()
    {
        return [.. _transactionTypes.Select(status => {
            return new TransactionType()
            {
                Name = status,
            };
        })];
    }
}