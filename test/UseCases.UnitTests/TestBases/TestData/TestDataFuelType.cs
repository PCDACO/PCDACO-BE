using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataFuelType
{
    private static FuelType CreateFuelType(string typeName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = typeName };

    public static async Task<FuelType> CreateTestFuelType(AppDBContext dBContext, string typeName)
    {
        var fuelType = CreateFuelType(typeName);

        await dBContext.FuelTypes.AddAsync(fuelType);
        await dBContext.SaveChangesAsync();

        return fuelType;
    }

    public static async Task<List<FuelType>> CreateTestFuelTypes(
        AppDBContext dBContext,
        List<string> typeNames
    )
    {
        var fuelTypes = typeNames.Select(typeName => CreateFuelType(typeName)).ToList();

        await dBContext.FuelTypes.AddRangeAsync(fuelTypes);
        await dBContext.SaveChangesAsync();

        return fuelTypes;
    }

    public static async Task<List<FuelType>> InitializeTestFuelTypes(AppDBContext dBContext)
    {
        List<string> typeNames = new() { "Gasoline", "Diesel", "Electric", "Hybrid" };

        var fuelTypes = new List<FuelType>();

        for (var i = 0; i < typeNames.Count; i++)
        {
            var fuelType = CreateFuelType(typeNames[i]);
            fuelTypes.Add(fuelType);
        }

        await dBContext.FuelTypes.AddRangeAsync(fuelTypes);
        await dBContext.SaveChangesAsync();

        return fuelTypes;
    }
}
