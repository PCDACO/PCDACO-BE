using Ardalis.Result;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        await TestDataCreateModel.CreateTestModels(_dbContext, manufacturer.Id, 3);
    }

    [Fact]
    public async Task Handle_NoModelsExist_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetAllModels.Handler(_dbContext);
        var query = new GetAllModels.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_ModelsExist_ReturnsPaginatedList()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetAllModels.Handler(_dbContext);
        var query = new GetAllModels.Query(PageNumber: 1, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.Equal("Test Model 3", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetAllModels.Handler(_dbContext);
        var query = new GetAllModels.Query(Name: "Model 2");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal("Test Model 2", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_ManufacturerNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        var manufacturer1 = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var manufacturer2 = new Manufacturer
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "Manufacturer 2",
            IsDeleted = false,
        };
        await _dbContext.Manufacturers.AddAsync(manufacturer2);
        await _dbContext.SaveChangesAsync();

        await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer1.Id);
        await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer2.Id);

        var handler = new GetAllModels.Handler(_dbContext);
        var query = new GetAllModels.Query(ManufacturerName: manufacturer1.Name);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(manufacturer1.Id, result.Value.Items.First().ManufacturerDetail?.Id);
    }

    [Fact]
    public async Task Handle_ReleaseDateFilter_ReturnsFilteredResults()
    {
        // Arrange
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var releaseDate = DateTimeOffset.UtcNow;
        var model = new Model
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            ManufacturerId = manufacturer.Id,
            Name = "Test Model",
            ReleaseDate = releaseDate,
        };
        await _dbContext.Models.AddAsync(model);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllModels.Handler(_dbContext);
        var query = new GetAllModels.Query(ReleaseDate: releaseDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(releaseDate.Date, result.Value.Items.First().ReleaseDate.Date);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetAllModels.Handler(_dbContext);
        var query = new GetAllModels.Query(PageNumber: 2, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Single(result.Value.Items);
        Assert.Equal("Test Model 1", result.Value.Items.First().Name);
    }
}
