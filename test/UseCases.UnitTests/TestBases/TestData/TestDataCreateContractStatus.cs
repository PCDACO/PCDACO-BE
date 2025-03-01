using Domain.Constants.EntityNames;
using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateContractStatus
{
    private static ContractStatus CreateContractStatus(string statusName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = statusName };

    public static async Task<List<ContractStatus>> InitializeTestContractStatus(
        AppDBContext dBContext
    )
    {
        List<string> statusNames =
        [
            ContractStatusNames.Pending,
            ContractStatusNames.Confirmed,
            ContractStatusNames.Cancelled
        ];

        var contractStatuses = new List<ContractStatus>();

        for (var i = 0; i < statusNames.Count; i++)
        {
            var contractStatus = CreateContractStatus(statusNames[i]);
            contractStatuses.Add(contractStatus);
        }

        await dBContext.ContractStatuses.AddRangeAsync(contractStatuses);
        await dBContext.SaveChangesAsync();

        return contractStatuses;
    }
}
