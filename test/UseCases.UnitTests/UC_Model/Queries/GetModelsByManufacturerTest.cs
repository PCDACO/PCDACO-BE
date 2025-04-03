using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Persistance.Data;
using UseCases.UC_Model.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Model.Queries;

[Collection("Test Collection")]
public class GetModelsByManufacturerTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_ManufacturerIdFilter_ReturnsFilteredResults()
    {
        // Arrange
        var manufacturer1 = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var manufacturer2 = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer1.Id);
        await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer2.Id);

        var handler = new GetModelsByManufacturer.Handler(_dbContext);
        var query = new GetModelsByManufacturer.Query(
            ManufacturerId: manufacturer1.Id,
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy danh sách mô hình xe thành công", result.SuccessMessage);
        Assert.Single(result.Value.Items);
        Assert.Equal(manufacturer1.Id, result.Value.Items.First().Manufacturer.Id);
    }

    [Fact]
    public async Task Handle_CombinedFilters_ReturnsFilteredResults()
    {
        // Arrange
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        // Create multiple models with different names
        var model1 = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var model2 = new Model
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            ManufacturerId = manufacturer.Id,
            Name = "Special Test Model",
            ReleaseDate = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };
        await _dbContext.Models.AddAsync(model2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetModelsByManufacturer.Handler(_dbContext);
        var query = new GetModelsByManufacturer.Query(
            ManufacturerId: manufacturer.Id,
            Name: "Special",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        var returnedModel = result.Value.Items.First();
        Assert.Equal("Special Test Model", returnedModel.Name);
        Assert.Equal(manufacturer.Id, result.Value.Items.First().Manufacturer.Id);
    }

    [Fact]
    public async Task Handle_NoElementsFound_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestData(); // Creates 3 test models
        var handler = new GetModelsByManufacturer.Handler(_dbContext);
        var query = new GetModelsByManufacturer.Query(
            ManufacturerId: Guid.Empty,
            Name: "Non Existent Model",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
        Assert.Equal("Lấy danh sách mô hình xe thành công", result.SuccessMessage);
    }
}
