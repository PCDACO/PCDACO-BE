using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateModel
{
    private static Model CreateModel(Guid manufacturerId, bool isDeleted = false) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            ManufacturerId = manufacturerId,
            Name = "Test Model",
            ReleaseDate = DateTimeOffset.UtcNow,
            IsDeleted = isDeleted,
        };

    public static async Task<Model> CreateTestModel(
        AppDBContext dBContext,
        Guid manufacturerId,
        bool isDeleted = false
    )
    {
        var model = CreateModel(manufacturerId, isDeleted);
        await dBContext.Models.AddAsync(model);
        await dBContext.SaveChangesAsync();

        return model;
    }

    public static async Task<List<Model>> CreateTestModels(
        AppDBContext dBContext,
        Guid manufacturerId,
        int count = 3
    )
    {
        var models = new List<Model>();
        for (var i = 0; i < count; i++)
        {
            var model = CreateModel(manufacturerId);
            model.Name = $"Test Model {i + 1}";
            models.Add(model);
        }

        await dBContext.Models.AddRangeAsync(models);
        await dBContext.SaveChangesAsync();

        return models;
    }
}
