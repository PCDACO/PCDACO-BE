using Domain.Entities;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Manufacturer.Queries;
using UseCases.UnitTests.TestBases;
using UUIDNext;

namespace UseCases.UnitTests.UC_Manufacturer.Queries;

[Collection("Test Collection")]
public class GetManufacturersTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private async Task SeedTestData()
    {
        var manufacturers = new[]
        {
            CreateCustomManufacturer("Toyota"),
            CreateCustomManufacturer("Honda"),
            CreateCustomManufacturer("Ford"),
        };

        await _dbContext.Manufacturers.AddRangeAsync(manufacturers);
        await _dbContext.SaveChangesAsync();
    }

    private static Manufacturer CreateCustomManufacturer(string name)
    {
        var manufacturer = new Manufacturer
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = name,
            IsDeleted = false,
        };

        return manufacturer;
    }

    [Fact]
    public async Task Handle_NoManufacturersExist_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetAllManufacturers.Handler(_dbContext);
        var query = new GetAllManufacturers.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_ManufacturersExist_ReturnsPaginatedList()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetAllManufacturers.Handler(_dbContext);
        var query = new GetAllManufacturers.Query(PageNumber: 1, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Value.TotalItems); // Total items in database
        Assert.Equal(2, result.Value.Items.Count()); // Items per page
        Assert.Equal("Ford", result.Value.Items.First().Name); // First item in descending order
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetAllManufacturers.Handler(_dbContext);
        var query = new GetAllManufacturers.Query(keyword: "hon"); // Partial keyword

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Value.Items);
        Assert.Equal("Honda", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedTestData();
        var handler = new GetAllManufacturers.Handler(_dbContext);
        var query = new GetAllManufacturers.Query(PageNumber: 2, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Single(result.Value.Items); // Page 2 has 1 item (3 total items, page size 2)
        Assert.Equal("Toyota", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_Ordering_ReturnsDescendingById()
    {
        // Arrange
        var manufacturer1 = CreateCustomManufacturer("Toyota");
        var manufacturer2 = CreateCustomManufacturer("Honda");
        var manufacturer3 = CreateCustomManufacturer("Ford");

        // Add to database
        await _dbContext.Manufacturers.AddRangeAsync([manufacturer1, manufacturer2, manufacturer3]);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllManufacturers.Handler(_dbContext);
        var query = new GetAllManufacturers.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var items = result.Value.Items.ToList();
        // Expect descending order: uuid3 (newest) -> uuid1 (oldest)
        Assert.Equal(manufacturer3.Id, items[0].Id);
        Assert.Equal(manufacturer2.Id, items[1].Id);
        Assert.Equal(manufacturer1.Id, items[2].Id);
    }
}
