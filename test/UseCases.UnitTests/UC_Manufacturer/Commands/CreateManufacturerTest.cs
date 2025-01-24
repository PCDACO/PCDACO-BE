using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Manufacturer.Commands;

namespace UseCases.UnitTests.UC_Manufacturer.Commands;

public class CreateManufacturerTest
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly CurrentUser _currentUser;

    public CreateManufacturerTest()
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
            Name = "Bla User",
            Email = "bla@gmail.com",
            Password = "12345",
            Role = role,
            Address = "200 Binh Quoi",
            DateOfBirth = DateTime.Now.AddYears(-30),
            Phone = "0938078946",
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.BuildMock().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver); // Non-admin role
        _currentUser.SetUser(testUser);

        var handler = new CreateManufacturer.Handler(_mockContext.Object, _currentUser);
        var command = new CreateManufacturer.Command("Toyota");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Bạn không có quyền thực hiện chức năng này !", result.Errors.First());
    }

    [Fact]
    public async Task Handle_AdminUser_CreatesManufacturerSuccessfully()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        // Mock Manufacturers DbSet
        var mockManufacturers = CreateMockDbSet(new List<Manufacturer>());
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        // Setup SaveChanges
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateManufacturer.Handler(_mockContext.Object, _currentUser);
        var command = new CreateManufacturer.Command("Toyota");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        mockManufacturers.Verify(
            m => m.AddAsync(It.IsAny<Manufacturer>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
