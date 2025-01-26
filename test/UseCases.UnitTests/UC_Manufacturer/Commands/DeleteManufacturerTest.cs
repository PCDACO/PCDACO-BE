using Ardalis.Result;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Manufacturer.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Manufacturer.Commands;

public class DeleteManufacturerTest : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        _currentUser.SetUser(testUser);

        var handler = new DeleteManufacturer.Handler(_dbContext, _currentUser);
        var command = new DeleteManufacturer.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền xóa hãng xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsNotFound()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new DeleteManufacturer.Handler(_dbContext, _currentUser);
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
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
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
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
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
