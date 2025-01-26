using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataTransmissionType
{
    private static TransmissionType CreateTransmissionType(string typeName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = typeName };

    public static async Task<TransmissionType> CreateTestTransmissionType(
        AppDBContext dBContext,
        string typeName
    )
    {
        var transmissionType = CreateTransmissionType(typeName);

        await dBContext.TransmissionTypes.AddAsync(transmissionType);
        await dBContext.SaveChangesAsync();

        return transmissionType;
    }

    public static async Task<List<TransmissionType>> CreateTestTransmissionTypes(
        AppDBContext dBContext,
        List<string> typeNames
    )
    {
        var transmissionTypes = typeNames
            .Select(typeName => CreateTransmissionType(typeName))
            .ToList();

        await dBContext.TransmissionTypes.AddRangeAsync(transmissionTypes);
        await dBContext.SaveChangesAsync();

        return transmissionTypes;
    }

    public static async Task<List<TransmissionType>> InitializeTestTransmissionTypes(
        AppDBContext dBContext
    )
    {
        List<string> typeNames = new() { "Automatic", "Manual" };

        var transmissionTypes = new List<TransmissionType>();

        for (var i = 0; i < typeNames.Count; i++)
        {
            var transmissionType = CreateTransmissionType(typeNames[i]);
            transmissionTypes.Add(transmissionType);
        }

        await dBContext.TransmissionTypes.AddRangeAsync(transmissionTypes);
        await dBContext.SaveChangesAsync();

        return transmissionTypes;
    }
}
