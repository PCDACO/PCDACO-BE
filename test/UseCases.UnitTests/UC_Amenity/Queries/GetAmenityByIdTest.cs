using Ardalis.Result;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.UC_Amenity.Queries;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Queries;

public class GetAmenityByIdTests : IAsyncLifetime
{
    private AppDBContext _dbContext;
    private readonly PostgreSqlContainer _postgresContainer;

    public GetAmenityByIdTests()
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

    private async Task<Amenity> CreateTestAmenity(bool isDeleted = false)
    {
        var amenity = new Amenity
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "WiFi",
            Description = "High-speed internet",
            IsDeleted = isDeleted
        };

        _dbContext.Amenities.Add(amenity);
        await _dbContext.SaveChangesAsync();
        return amenity;
    }

    [Fact]
    public async Task Handle_AmenityExists_ReturnsAmenity()
    {
        // Arrange
        var testAmenity = await CreateTestAmenity();
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
        var testAmenity = await CreateTestAmenity(isDeleted: true);
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
