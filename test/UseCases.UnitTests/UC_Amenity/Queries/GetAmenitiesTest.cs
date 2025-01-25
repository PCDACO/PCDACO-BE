using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.UC_Amenity.Queries;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Queries;

public class GetAmenitiesTests
{
    private readonly Mock<IAppDBContext> _mockContext;

    public GetAmenitiesTests()
    {
        _mockContext = new Mock<IAppDBContext>();
    }

    private static List<Amenity> CreateTestAmenities()
    {
        return
        [
            new()
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "WiFi",
                Description = "High-speed internet",
                IsDeleted = false,
            },
            new Amenity
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Air Conditioning",
                Description = "Cooling system",
                IsDeleted = false,
            },
            new Amenity
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Parking",
                Description = "Car parking space",
                IsDeleted = false,
            },
        ];
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.AsQueryable().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_NoAmenitiesExist_ReturnsEmptyList()
    {
        // Arrange
        var mockAmenities = CreateMockDbSet(new List<Amenity>());
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenities.Handler(_mockContext.Object);
        var query = new GetAmenities.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_AmenitiesExist_ReturnsPaginatedList()
    {
        // Arrange
        var testAmenities = CreateTestAmenities();
        var mockAmenities = CreateMockDbSet(testAmenities);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenities.Handler(_mockContext.Object);
        var query = new GetAmenities.Query(PageNumber: 1, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Value.TotalItems); // Total items in database
        Assert.Equal(2, result.Value.Items.Count()); // Items per page
        Assert.Equal("Parking", result.Value.Items.First().Name); // First item in descending order
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        var testAmenities = CreateTestAmenities();
        var mockAmenities = CreateMockDbSet(testAmenities);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenities.Handler(_mockContext.Object);
        var query = new GetAmenities.Query(keyword: "con"); // Partial keyword

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Value.Items);
        Assert.Equal("Air Conditioning", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var testAmenities = CreateTestAmenities();
        var mockAmenities = CreateMockDbSet(testAmenities);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenities.Handler(_mockContext.Object);
        var query = new GetAmenities.Query(PageNumber: 2, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Single(result.Value.Items); // Page 2 has 1 item (3 total items, page size 2)
        Assert.Equal("WiFi", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_Ordering_ReturnsDescendingById()
    {
        // Arrange
        var testAmenities = CreateTestAmenities();

        // Explicitly set IDs to control order
        var uuid = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        testAmenities[2].Id = uuid;

        var mockAmenities = CreateMockDbSet(testAmenities);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenities.Handler(_mockContext.Object);
        var query = new GetAmenities.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // Expect descending order: Id3, Id2, Id1
        Assert.Equal(uuid.ToString(), result.Value.Items.First().Id.ToString());
    }
}
