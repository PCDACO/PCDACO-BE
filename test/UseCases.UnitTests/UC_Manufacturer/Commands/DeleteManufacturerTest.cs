using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Manufacturer.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Manufacturer.Commands;

public class DeleteManufacturerTest : DatabaseTestBase
{
    [Fact(Timeout = 3000)]
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new DeleteManufacturer.Handler(_dbContext, _currentUser);
        var command = new DeleteManufacturer.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền xóa hãng xe", result.Errors);
    }

    [Fact(Timeout = 3000)]
    public async Task Handle_ManufacturerNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new DeleteManufacturer.Handler(_dbContext, _currentUser);
        var command = new DeleteManufacturer.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }

    [Fact(Timeout = 3000)]
    public async Task Handle_ValidRequest_DeletesManufacturerSuccessfully()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        var handler = new DeleteManufacturer.Handler(_dbContext, _currentUser);
        var command = new DeleteManufacturer.Command(testManufacturer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Xóa hãng xe thành công", result.SuccessMessage);

        // Verify soft delete
        var deletedManufacturer = await _dbContext
            .Manufacturers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == testManufacturer.Id);

        Assert.True(deletedManufacturer?.IsDeleted);
    }

    [Fact]
    public async Task Handle_ManufacturerAlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            isDeleted: true
        );

        var handler = new DeleteManufacturer.Handler(_dbContext, _currentUser);
        var command = new DeleteManufacturer.Command(testManufacturer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }
}
