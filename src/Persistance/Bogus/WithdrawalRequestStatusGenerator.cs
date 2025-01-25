using Domain.Entities;

namespace Persistance.Bogus;

public class WithdrawalRequestStatusGenerator
{
    private static readonly string[] _withdrawalRequestStatus = ["Pending", "Completed", "Rejected"];
    public static WithdrawalRequestStatus[] Execute()
    {
        return [.. _withdrawalRequestStatus.Select(status => {
            return new WithdrawalRequestStatus()
            {
                Name = status,
            };
        })];
    }
}