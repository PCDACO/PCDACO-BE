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

public class DeleteManufacturerTest
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly CurrentUser _currentUser;

    public DeleteManufacturerTest()
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

    private static Manufacturer CreateTestManufacturer()
    {
        return new Manufacturer
        {
            Id = Guid.NewGuid(),
            Name = "Toyota",
            IsDeleted = false,
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
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver); // Non-admin user
        _currentUser.SetUser(testUser);

        var handler = new DeleteManufacturer.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteManufacturer.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Bạn không có quyền xóa hãng xe", result.Errors.First());
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsNotFound()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        // Mock empty Manufacturers DbSet
        var mockManufacturers = CreateMockDbSet(new List<Manufacturer>());
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new DeleteManufacturer.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteManufacturer.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesManufacturerSuccessfully()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var testManufacturer = CreateTestManufacturer();
        var mockManufacturers = CreateMockDbSet([testManufacturer]);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteManufacturer.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteManufacturer.Command(testManufacturer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Xóa hãng xe thành công", result.SuccessMessage);
        Assert.True(testManufacturer.IsDeleted); // Verify soft delete
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ManufacturerAlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var testManufacturer = CreateTestManufacturer();
        testManufacturer.IsDeleted = true; // Mark as already deleted

        // Mock Manufacturers DbSet to return nothing (simulate query filter excluding deleted entities)
        var mockManufacturers = CreateMockDbSet(new List<Manufacturer>());
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new DeleteManufacturer.Handler(_mockContext.Object, _currentUser);
        var command = new DeleteManufacturer.Command(testManufacturer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("Không tìm thấy hãng xe", result.Errors.First());
    }
}
