using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCarStatus
{
    private static CarStatus CreateCarStatus(string statusName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = statusName };

    public static async Task<CarStatus> CreateTestCarStatus(
        AppDBContext dBContext,
        string statusName
    )
    {
        var carStatus = CreateCarStatus(statusName);

        await dBContext.CarStatuses.AddAsync(carStatus);
        await dBContext.SaveChangesAsync();

        return carStatus;
    }

    public static async Task<List<CarStatus>> CreateTestCarStatuses(
        AppDBContext dBContext,
        List<string> statusNames
    )
    {
        var carStatuses = statusNames.Select(CreateCarStatus).ToList();

        await dBContext.CarStatuses.AddRangeAsync(carStatuses);
        await dBContext.SaveChangesAsync();

        return carStatuses;
    }

    public static async Task<List<CarStatus>> InitializeTestCarStatuses(AppDBContext dBContext)
    {
        List<string> statusNames = ["Available", "Rented", "Inactive"];

        var carStatuses = new List<CarStatus>();

        for (var i = 0; i < statusNames.Count; i++)
        {
            var carStatus = CreateCarStatus(statusNames[i]);
            carStatuses.Add(carStatus);
        }

        await dBContext.CarStatuses.AddRangeAsync(carStatuses);
        await dBContext.SaveChangesAsync();

        return carStatuses;
    }
}
