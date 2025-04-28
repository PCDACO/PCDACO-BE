using Domain.Constants;
using Domain.Entities;

namespace Persistance.Bogus;

public class TransactionTypeGenerator
{
    private static readonly string[] _transactionTypes =
    [
        TransactionTypeNames.BookingPayment,
        TransactionTypeNames.ExtensionPayment,
        TransactionTypeNames.PlatformFee,
        TransactionTypeNames.OwnerEarning,
        TransactionTypeNames.Withdrawal,
        TransactionTypeNames.Refund,
        TransactionTypeNames.Compensation,
    ];

    public static TransactionType[] Execute() =>
        [.. _transactionTypes.Select(status => new TransactionType() { Name = status, })];
}
