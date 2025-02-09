using Ardalis.Result;
using Domain.Entities;
using Persistance.Data;
using UseCases.UC_Model.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Model.Queries;

[Collection("Test Collection")]
public class GetModelByIdTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ModelExists_ReturnsModel()
    {
        // Arrange
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var handler = new GetModelById.Handler(_dbContext);
        var query = new GetModelById.Query(model.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy thông tin mô hình xe thành công", result.SuccessMessage);
        Assert.Equal(model.Id, result.Value.Id);
        Assert.Equal(model.Name, result.Value.Name);
        Assert.Equal(model.ReleaseDate.Date, result.Value.ReleaseDate.Date);
        Assert.Equal(manufacturer.Id, result.Value.Manufacturer.Id);
        Assert.Equal(manufacturer.Name, result.Value.Manufacturer.Name);
    }

    [Fact]
    public async Task Handle_ModelNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetModelById.Handler(_dbContext);
        var query = new GetModelById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy mô hình xe", result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedModel_ReturnsNotFound()
    {
        // Arrange
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(
            _dbContext,
            manufacturer.Id,
            isDeleted: true
        );

        var handler = new GetModelById.Handler(_dbContext);
        var query = new GetModelById.Query(model.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy mô hình xe", result.Errors);
    }

    [Fact]
    public async Task Handle_IncludesManufacturerDetails()
    {
        // Arrange
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Test Manufacturer"
        );
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var handler = new GetModelById.Handler(_dbContext);
        var query = new GetModelById.Query(model.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value.Manufacturer);
        Assert.Equal(manufacturer.Id, result.Value.Manufacturer.Id);
        Assert.Equal(manufacturer.Name, result.Value.Manufacturer.Name);
    }
}
