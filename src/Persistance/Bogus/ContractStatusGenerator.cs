using Domain.Entities;

namespace Persistance.Bogus;

public class ContractStatusGenerator
{
    private static readonly string[] _contractStatus = ["Pending", "Confirmed", "Cancelled"];
    public static ContractStatus[] Execute()
    {
        return [.. _contractStatus.Select(status => {
            return new ContractStatus()
            {
                Name = status,
            };
        })];
    }
}