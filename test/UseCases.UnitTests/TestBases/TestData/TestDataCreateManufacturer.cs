using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateManufacturer
{
    private static Manufacturer CreateManufacturer(string name, bool isDeleted = false) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = name,
            IsDeleted = isDeleted,
        };

    public static async Task<Manufacturer> CreateTestManufacturer(
        AppDBContext dBContext,
        bool isDeleted = false
    )
    {
        var manufacturer = CreateManufacturer("Test Manufacturer", isDeleted);
        await dBContext.Manufacturers.AddAsync(manufacturer);
        await dBContext.SaveChangesAsync();

        return manufacturer;
    }

    public static async Task<Manufacturer> CreateTestManufacturer(
        AppDBContext dBContext,
        string name,
        bool isDeleted = false
    )
    {
        var manufacturer = CreateManufacturer(name, isDeleted);
        await dBContext.Manufacturers.AddAsync(manufacturer);
        await dBContext.SaveChangesAsync();

        return manufacturer;
    }

    public static async Task<List<Manufacturer>> CreateTestManufacturers(AppDBContext dBContext)
    {
        var manufacturers = new[]
        {
            CreateManufacturer("Toyota"),
            CreateManufacturer("Honda"),
            CreateManufacturer("Ford"),
        };

        await dBContext.Manufacturers.AddRangeAsync(manufacturers);
        await dBContext.SaveChangesAsync();

        return manufacturers.ToList();
    }
}
