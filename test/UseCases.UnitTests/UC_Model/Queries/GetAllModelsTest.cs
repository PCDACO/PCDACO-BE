using Ardalis.Result;
using Domain.Entities;
using Persistance.Data;
using UseCases.UC_Model.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Model.Queries;

[Collection("Test Collection")]
public class GetAllModelsTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private async Task SeedTestData()
    {
        var manufacturer1 = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Toyota"
        );
        var manufacturer2 = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Honda"
        );

        // Create test models for Toyota
        await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer1.Id);
        var specialModel = new Model
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            ManufacturerId = manufacturer1.Id,
            Name = "Special Camry",
            ReleaseDate = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };
        await _dbContext.Models.AddAsync(specialModel);

        // Create test models for Honda
        await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer2.Id);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_NoKeyword_ReturnsAllModels()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetModels.Handler(_dbContext);
        var query = new GetModels.Query(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal("Lấy danh sách mô hình xe thành công", result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_ModelNameKeyword_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetModels.Handler(_dbContext);
        var query = new GetModels.Query(PageNumber: 1, PageSize: 10, Keyword: "Special");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Contains(result.Value.Items, m => m.Name.Contains("Special"));
    }

    [Fact]
    public async Task Handle_ManufacturerNameKeyword_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetModels.Handler(_dbContext);
        var query = new GetModels.Query(PageNumber: 1, PageSize: 10, Keyword: "Toyota");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.All(result.Value.Items, m => Assert.Equal("Toyota", m.Manufacturer.Name));
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectItems()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetModels.Handler(_dbContext);
        var query = new GetModels.Query(PageNumber: 1, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.True(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_DeletedModels_AreNotReturned()
    {
        // Arrange
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(
            _dbContext,
            manufacturer.Id,
            isDeleted: true
        );

        var handler = new GetModels.Handler(_dbContext);
        var query = new GetModels.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetModels.Handler(_dbContext);
        var query = new GetModels.Query(Keyword: "NonExistentModel");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
        Assert.False(result.Value.HasNext);
    }
}
