using Ardalis.Result;
using Persistance.Data;
using UseCases.UC_Amenity.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Queries;

[Collection("Test Collection")]
public class GetAmenityByIdTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_AmenityExists_ReturnsAmenity()
    {
        // Arrange
        var testAmenity = await TestDataCreateAmenity.CreateTestAmenity(
            _dbContext,
            isDeleted: false
        );
        var handler = new GetAmenityById.Handler(_dbContext);
        var command = new GetAmenityById.Query(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(testAmenity.Id, result.Value.Id);
        Assert.Equal(testAmenity.Name, result.Value.Name);
    }

    [Fact]
    public async Task Handle_AmenityIsDeleted_ReturnsNotFound()
    {
        // Arrange
        var testAmenity = await TestDataCreateAmenity.CreateTestAmenity(
            _dbContext,
            isDeleted: true
        );
        var handler = new GetAmenityById.Handler(_dbContext);
        var command = new GetAmenityById.Query(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetAmenityById.Handler(_dbContext);
        var command = new GetAmenityById.Query(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }
}
