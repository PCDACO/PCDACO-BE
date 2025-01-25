using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.UC_Manufacturer.Queries;
using UUIDNext;
using Xunit;

namespace UseCases.UnitTests.UC_Manufacturer.Queries;

public class GetManufacturersTest
{
    private readonly Mock<IAppDBContext> _mockContext;

    public GetManufacturersTest()
    {
        _mockContext = new Mock<IAppDBContext>();
    }

    private static List<Manufacturer> CreateTestManufacturers()
    {
        return new List<Manufacturer>
        {
            new Manufacturer
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Toyota",
                IsDeleted = false,
            },
            new Manufacturer
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Honda",
                IsDeleted = false,
            },
            new Manufacturer
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Ford",
                IsDeleted = false,
            },
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.AsQueryable().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_NoManufacturersExist_ReturnsEmptyList()
    {
        // Arrange
        var mockManufacturers = CreateMockDbSet(new List<Manufacturer>());
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new GetAllManufacturers.Handler(_mockContext.Object);
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
        var testManufacturers = CreateTestManufacturers();
        var mockManufacturers = CreateMockDbSet(testManufacturers);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new GetAllManufacturers.Handler(_mockContext.Object);
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
        var testManufacturers = CreateTestManufacturers();
        var mockManufacturers = CreateMockDbSet(testManufacturers);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new GetAllManufacturers.Handler(_mockContext.Object);
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
        var testManufacturers = CreateTestManufacturers();
        var mockManufacturers = CreateMockDbSet(testManufacturers);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new GetAllManufacturers.Handler(_mockContext.Object);
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
        var testManufacturers = CreateTestManufacturers();

        // Explicitly set IDs to control order
        var uuid = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        testManufacturers[2].Id = uuid;

        var mockManufacturers = CreateMockDbSet(testManufacturers);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new GetAllManufacturers.Handler(_mockContext.Object);
        var query = new GetAllManufacturers.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // Expect descending order: Id3, Id2, Id1
        Assert.Equal(uuid.ToString(), result.Value.Items.First().Id.ToString());
    }
}
