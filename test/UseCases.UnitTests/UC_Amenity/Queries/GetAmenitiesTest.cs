using UseCases.UC_Amenity.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Amenity.Queries;

public class GetAmenitiesTests : DatabaseTestBase
{
    [Fact]
    public async Task Handle_NoAmenitiesExist_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetAmenities.Handler(_dbContext);
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
        await TestDataCreateAmenity.CreateTestAmenities(_dbContext);
        var handler = new GetAmenities.Handler(_dbContext);
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
        await TestDataCreateAmenity.CreateTestAmenities(_dbContext);
        var handler = new GetAmenities.Handler(_dbContext);
        var query = new GetAmenities.Query(keyword: "con");

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
        await TestDataCreateAmenity.CreateTestAmenities(_dbContext);
        var handler = new GetAmenities.Handler(_dbContext);
        var query = new GetAmenities.Query(PageNumber: 2, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Single(result.Value.Items);
        Assert.Equal("WiFi", result.Value.Items.First().Name);
    }
}
