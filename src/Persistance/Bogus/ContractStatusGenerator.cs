using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class ContractStatusGenerator
{
    private static readonly string[] _contractStatus =
    [
        ContractStatusNames.Pending,
        ContractStatusNames.Confirmed,
        ContractStatusNames.Cancelled
    ];

    public static ContractStatus[] Execute()
    {
        return
        [
            .. _contractStatus.Select(status =>
            {
                return new ContractStatus() { Name = status, };
            })
        ];
    }
}
