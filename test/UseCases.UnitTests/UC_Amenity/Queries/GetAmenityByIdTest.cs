using Ardalis.Result;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.UC_Amenity.Queries;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Queries;

public class GetAmenityByIdTests
{
    private readonly Mock<IAppDBContext> _mockContext;

    public GetAmenityByIdTests()
    {
        _mockContext = new Mock<IAppDBContext>();
    }

    private static Amenity CreateTestAmenity(bool isDeleted = false)
    {
        return new Amenity
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "WiFi",
            Description = "High-speed internet",
            IsDeleted = isDeleted
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.AsQueryable().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_AmenityExists_ReturnsAmenity()
    {
        // Arrange
        var testAmenity = CreateTestAmenity();
        var mockAmenities = CreateMockDbSet([testAmenity]);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenityById.Handler(_mockContext.Object);
        var command = new GetAmenityById.Query(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(testAmenity.Id, result.Value.Id);
        Assert.Equal(testAmenity.Name, result.Value.Name);
        Assert.Equal(testAmenity.Description, result.Value.Description);
    }

    [Fact]
    public async Task Handle_AmenityIsDeleted_StillReturnsAmenity()
    {
        // Arrange
        var testAmenity = CreateTestAmenity(isDeleted: true); // Deleted amenity
        var mockAmenities = CreateMockDbSet([testAmenity]);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenityById.Handler(_mockContext.Object);
        var command = new GetAmenityById.Query(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert (handler returns deleted amenities based on current code)
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(testAmenity.Id, result.Value.Id);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockAmenities = CreateMockDbSet(new List<Amenity>());
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new GetAmenityById.Handler(_mockContext.Object);
        var command = new GetAmenityById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }
}
