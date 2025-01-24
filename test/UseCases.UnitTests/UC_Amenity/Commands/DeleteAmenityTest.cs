using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Amenity.Commands;

namespace UseCases.UnitTests.UC_Amenity.Commands;

public class DeleteAmenityTests
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly CurrentUser _currentUser;

    public DeleteAmenityTests()
    {
        _mockContext = new Mock<IAppDBContext>();
        _currentUser = new CurrentUser();
    }

    private static User CreateTestUser(UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            EncryptionKeyId = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Password = "password",
            Role = role,
            Address = "Test Address",
            DateOfBirth = DateTime.Now.AddYears(-30),
            Phone = "1234567890"
        };
    }

    private static Amenity CreateTestAmenity()
    {
        return new Amenity
        {
            Id = Guid.NewGuid(),
            Name = "WiFi",
            Description = "High-speed internet",
            IsDeleted = false
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        // Use MockQueryable.Moq to create a mock DbSet from in-memory data
        var mockSet = data.BuildMock().BuildMockDbSet();

        // Setup FindAsync to return the first item in the list (simulates EF's FindAsync)
        mockSet
            .Setup(x => x.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] ids) => data.FirstOrDefault());

        return mockSet;
    }

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver); // Non-admin user
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteAmenity.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền xóa tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        // Mock empty Amenities DbSet
        var mockAmenities = CreateMockDbSet(new List<Amenity>());
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new DeleteAmenity.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteAmenity.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesAmenitySuccessfully()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var testAmenity = CreateTestAmenity();
        var mockAmenities = CreateMockDbSet([testAmenity]);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteAmenity.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteAmenity.Command(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Xóa tiện nghi thành công", result.SuccessMessage);
        Assert.True(testAmenity.IsDeleted); // Verify soft delete
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AmenityAlreadyDeleted_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var testAmenity = CreateTestAmenity();
        testAmenity.IsDeleted = true; // Mark as already deleted

        var mockAmenities = CreateMockDbSet([testAmenity]);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        var handler = new DeleteAmenity.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteAmenity.Command(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }
}
