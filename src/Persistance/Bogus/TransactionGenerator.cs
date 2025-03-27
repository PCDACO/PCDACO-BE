using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class TransactionGenerator
{
    private sealed record TransactionDummy
    {
        public required decimal Amount { get; set; }
        public required string Description { get; set; }
        public required TransactionStatusEnum Status { get; set; }
        public required string TypeName { get; set; }
    }

    public static Transaction[] Execute(
        User[] users,
        TransactionType[] transactionTypes,
        BankAccount[] bankAccounts
    )
    {
        var dummyData = new TransactionDummy[]
        {
            new()
            {
                Amount = 1000000,
                Description = "Withdrawal to bank account",
                Status = TransactionStatusEnum.Completed,
                TypeName = TransactionTypeNames.Withdrawal
            },
            new()
            {
                Amount = 500000,
                Description = "Platform fee for booking",
                Status = TransactionStatusEnum.Pending,
                TypeName = TransactionTypeNames.PlatformFee
            },
            new()
            {
                Amount = 2000000,
                Description = "Car rental payment",
                Status = TransactionStatusEnum.Completed,
                TypeName = TransactionTypeNames.BookingPayment
            }
        };

        return
        [
            .. dummyData.Select(t => new Transaction
            {
                FromUserId = users[0].Id,
                ToUserId = users[1].Id,
                BookingId = null, // You might want to link this to actual bookings
                BankAccountId =
                    t.TypeName == TransactionTypeNames.Withdrawal ? bankAccounts[0].Id : null,
                TypeId = transactionTypes.First(tt => tt.Name == t.TypeName).Id,
                Status = t.Status,
                Amount = t.Amount,
                Description = t.Description,
                BalanceAfter = 5000000, // This should be calculated based on actual balance
                ProofUrl = string.Empty
            })
        ];
    }
}
