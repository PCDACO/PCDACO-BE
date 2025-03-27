using Domain.Entities;
using Domain.Enums;

namespace Persistance.Bogus;

public class WithdrawalRequestGenerator
{
    private sealed record WithdrawalRequestDummy
    {
        public required decimal Amount { get; set; }
        public required WithdrawRequestStatusEnum Status { get; set; }
        public required string RejectReason { get; set; }
    }

    public static WithdrawalRequest[] Execute(User[] users, BankAccount[] bankAccounts)
    {
        var dummyData = new WithdrawalRequestDummy[]
        {
            new()
            {
                Amount = 1000000,
                Status = WithdrawRequestStatusEnum.Completed,
                RejectReason = string.Empty
            },
            new()
            {
                Amount = 500000,
                Status = WithdrawRequestStatusEnum.Pending,
                RejectReason = string.Empty
            },
            new()
            {
                Amount = 750000,
                Status = WithdrawRequestStatusEnum.Rejected,
                RejectReason = "Insufficient balance"
            }
        };

        return
        [
            .. dummyData.Select(wd => new WithdrawalRequest
            {
                UserId = users[0].Id, // You might want to randomize this
                BankAccountId = bankAccounts[0].Id, // You might want to randomize this
                Amount = wd.Amount,
                Status = wd.Status,
                RejectReason = wd.RejectReason,
                ProcessedAt =
                    wd.Status != WithdrawRequestStatusEnum.Pending ? DateTimeOffset.UtcNow : null,
                ProcessedByAdminId =
                    wd.Status != WithdrawRequestStatusEnum.Pending ? users[1].Id : null
            })
        ];
    }
}
