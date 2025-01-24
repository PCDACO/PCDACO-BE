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

public class CreateAmenityTest
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly CurrentUser _currentUser;

    public CreateAmenityTest()
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

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.BuildMock().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver); // Non-admin role
        _currentUser.SetUser(testUser);

        var handler = new CreateAmenity.Handler(_mockContext.Object, _currentUser);
        var command = new CreateAmenity.Command("WiFi", "High-speed internet");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("Bạn không có quyền thực hiện thao tác này", result.Errors.First());
    }

    [Fact]
    public async Task Handle_AdminUser_CreatesAmenitySuccessfully()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        // Mock Amenities DbSet
        var mockAmenities = CreateMockDbSet(new List<Amenity>());
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        // Setup SaveChanges
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateAmenity.Handler(_mockContext.Object, _currentUser);
        var command = new CreateAmenity.Command("WiFi", "High-speed internet");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Created, result.Status);
        mockAmenities.Verify(
            m => m.AddAsync(It.IsAny<Amenity>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
