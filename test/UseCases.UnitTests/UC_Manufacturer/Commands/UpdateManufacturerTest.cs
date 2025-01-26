using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using UseCases.UC_Manufacturer.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Manufacturer.Commands;

public class UpdateManufacturerTest : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateManufacturer.Handler(_dbContext, _currentUser);
        var command = new UpdateManufacturer.Command(Guid.NewGuid(), "New Name");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện thao tác này", result.Errors);
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateManufacturer.Handler(_dbContext, _currentUser);
        var command = new UpdateManufacturer.Command(Guid.NewGuid(), "New Name");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesManufacturerSuccessfully()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        var handler = new UpdateManufacturer.Handler(_dbContext, _currentUser);
        var command = new UpdateManufacturer.Command(testManufacturer.Id, "New Name");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Cập nhật hãng xe thành công", result.SuccessMessage);

        // Verify database updates
        var updatedManufacturer = await _dbContext.Manufacturers.FindAsync(testManufacturer.Id);
        Assert.NotNull(updatedManufacturer);
        Assert.Equal("New Name", updatedManufacturer.Name);
        Assert.True(DateTimeOffset.UtcNow >= updatedManufacturer.UpdatedAt);
    }
}
