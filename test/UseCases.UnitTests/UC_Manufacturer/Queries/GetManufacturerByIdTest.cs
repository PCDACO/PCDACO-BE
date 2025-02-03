using Ardalis.Result;
using Persistance.Data;
using UseCases.UC_Manufacturer.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Manufacturer.Queries;

[Collection("Test Collection")]
public class GetManufacturerByIdTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ManufacturerExists_ReturnsManufacturer()
    {
        // Arrange
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            dBContext: _dbContext,
            isDeleted: false
        );
        var handler = new GetManufacturerById.Handler(_dbContext);
        var query = new GetManufacturerById.Query(testManufacturer.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy thông tin hãng xe thành công", result.SuccessMessage);
        Assert.Equal(testManufacturer.Id, result.Value.Id);
        Assert.Equal(testManufacturer.Name, result.Value.Name);
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetManufacturerById.Handler(_dbContext);
        var query = new GetManufacturerById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ManufacturerIsDeleted_ReturnsNotFound()
    {
        // Arrange
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            dBContext: _dbContext,
            isDeleted: true
        );
        var handler = new GetManufacturerById.Handler(_dbContext);
        var query = new GetManufacturerById.Query(testManufacturer.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }
}
