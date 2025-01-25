using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.Abstractions;
using UseCases.UC_Amenity.Queries;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Queries;

public class GetAmenitiesTests : IAsyncLifetime
{
    private AppDBContext _dbContext;
    private readonly PostgreSqlContainer _postgresContainer;

    public GetAmenitiesTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:latest")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDBContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString(), o => o.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new AppDBContext(options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task SeedTestData()
    {
        var amenities = new[]
        {
            new Amenity
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "WiFi",
                Description = "High-speed internet",
                IsDeleted = false
            },
            new Amenity
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Air Conditioning",
                Description = "Cooling system",
                IsDeleted = false
            },
            new Amenity
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Parking",
                Description = "Car parking space",
                IsDeleted = false
            }
        };

        await _dbContext.Amenities.AddRangeAsync(amenities);
        await _dbContext.SaveChangesAsync();
    }

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
        await SeedTestData();
        var handler = new GetAmenities.Handler(_dbContext);
        var query = new GetAmenities.Query(PageNumber: 1, PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(3, result.Value.Items.Count());
        Assert.Equal("Parking", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestData();
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
        await SeedTestData();
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
