using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateManufacturer
{
    private static Manufacturer CreateManufacturer(bool isDeleted = false) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "Test Manufacturer",
            IsDeleted = isDeleted
        };

    public static async Task<Manufacturer> CreateTestManufacturer(
        AppDBContext dBContext,
        bool isDeleted = false
    )
    {
        var manufacturer = CreateManufacturer(isDeleted);
        await dBContext.Manufacturers.AddAsync(manufacturer);
        await dBContext.SaveChangesAsync();

        return manufacturer;
    }
}
