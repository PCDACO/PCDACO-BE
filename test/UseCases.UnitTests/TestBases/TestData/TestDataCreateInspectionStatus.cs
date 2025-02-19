using Domain.Constants;
using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateInspectionStatus
{
    private static InspectionStatus CreateInspectionStatus(string name) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = name };

    public static async Task<InspectionStatus> CreateTestInspectionStatus(
        AppDBContext dBContext,
        string name = InspectionStatusNames.Pending
    )
    {
        var status = CreateInspectionStatus(name);
        await dBContext.InspectionStatuses.AddAsync(status);
        await dBContext.SaveChangesAsync();
        return status;
    }

    public static async Task<List<InspectionStatus>> CreateTestInspectionStatuses(
        AppDBContext dBContext
    )
    {
        var statusNames = new[]
        {
            InspectionStatusNames.Pending,
            InspectionStatusNames.Scheduled,
            InspectionStatusNames.InProgress,
            InspectionStatusNames.Approved,
            InspectionStatusNames.Rejected,
        };

        var statuses = statusNames.Select(CreateInspectionStatus).ToList();
        await dBContext.InspectionStatuses.AddRangeAsync(statuses);
        await dBContext.SaveChangesAsync();
        return statuses;
    }
}
